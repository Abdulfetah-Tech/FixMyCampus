using System.Linq.Expressions;
using FixMyCampus.Application.Common;
using FixMyCampus.Application.DTOs.Tickets;
using FixMyCampus.Application.Interfaces;
using FixMyCampus.Domain.Entities;
using FixMyCampus.Domain.Enums;
using FixMyCampus.Domain.Rules;
using FixMyCampus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixMyCampus.Infrastructure.Services;

public class TicketService(AppDbContext db) : ITicketService
{
    // Urgency is stored as text, so ORDER BY would sort alphabetically
    // (High, Low, Medium).
    // This expression ranks it properly:
    // High = 3, Medium = 2, Low = 1.
    private static readonly Expression<Func<Ticket, int>> UrgencyRank = t =>
        t.Urgency == TicketUrgency.High ? 3 :
        t.Urgency == TicketUrgency.Medium ? 2 : 1;

    // ---------- Create ----------

    public async Task<TicketDetailResponse> CreateAsync(
        CreateTicketRequest request,
        string reporterId)
    {
        var isApproved = await db.Users.AnyAsync(u =>
            u.Id == reporterId &&
            u.ApprovalStatus == ApprovalStatus.Approved);

        if (!isApproved)
        {
            throw new UnauthorizedAccessException(
                "Your account is not approved to report issues.");
        }

        if (!await db.Buildings.AnyAsync(b => b.Id == request.BuildingId))
        {
            throw new ArgumentException(
                "The selected building does not exist.");
        }

        var ticket = new Ticket
        {
            Category = request.Category,
            BuildingId = request.BuildingId,
            Room = request.Room.Trim(),
            Description = request.Description.Trim(),
            Urgency = request.Urgency,
            Status = TicketStatus.New,
            ReporterId = reporterId
        };

        ticket.StatusHistory.Add(new TicketStatusHistory
        {
            FromStatus = null,
            ToStatus = TicketStatus.New,
            ChangedByUserId = reporterId,
            Note = "Ticket created"
        });

        db.Tickets.Add(ticket);

        await db.SaveChangesAsync();

        return await GetByIdAsync(ticket.Id);
    }

    // ---------- Read ----------

    public async Task<PagedResult<TicketListItemResponse>> GetFeedAsync(
        TicketQueryParameters query)
    {
        var tickets = ApplyFilters(
            db.Tickets.AsNoTracking(),
            query);

        return await ToPagedAsync(tickets, query);
    }

    public async Task<PagedResult<TicketListItemResponse>> GetMineAsync(
        string reporterId,
        TicketQueryParameters query)
    {
        var tickets = ApplyFilters(
            db.Tickets
                .AsNoTracking()
                .Where(t => t.ReporterId == reporterId),
            query);

        return await ToPagedAsync(tickets, query);
    }

    public async Task<TicketDetailResponse> GetByIdAsync(int id)
    {
        var ticket = await db.Tickets
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Building)
            .Include(t => t.Reporter)
            .Include(t => t.AssignedTechnician)
            .Include(t => t.StatusHistory)
                .ThenInclude(h => h.ChangedByUser)
            .Include(t => t.Attachments)
                .ThenInclude(a => a.UploadedByUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            throw new KeyNotFoundException(
                $"Ticket {id} was not found.");
        }

        return ToDetailResponse(ticket);
    }

    public async Task<IReadOnlyList<TicketListItemResponse>> FindSimilarOpenAsync(
        int buildingId,
        string room,
        TicketCategory category)
    {
        var normalizedRoom = room.Trim().ToLower();

        var query = db.Tickets
            .AsNoTracking()
            .Where(t =>
                t.BuildingId == buildingId &&
                t.Category == category &&
                t.Status != TicketStatus.Resolved &&
                t.Room.ToLower() == normalizedRoom)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5);

        return await ToListItemsAsync(query);
    }

    // ---------- Workflow ----------

    public async Task<TicketDetailResponse> AssignAsync(
        int id,
        AssignTicketRequest request,
        string adminId)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            throw new KeyNotFoundException(
                $"Ticket {id} was not found.");
        }

        if (!TicketWorkflow.CanMove(
                ticket.Status,
                TicketStatus.Assigned))
        {
            throw new InvalidOperationException(
                $"Only a New ticket can be assigned. " +
                $"This ticket is already {Display(ticket.Status)}.");
        }

        var technician = await db.Technicians
            .FirstOrDefaultAsync(t =>
                t.Id == request.TechnicianId &&
                t.IsActive);

        if (technician is null)
        {
            throw new ArgumentException(
                "The selected technician does not exist or is not active.");
        }

        var now = DateTime.UtcNow;
        var previous = ticket.Status;

        ticket.Status = TicketStatus.Assigned;
        ticket.AssignedTechnicianId = technician.Id;
        ticket.AssignedAt = now;
        ticket.UpdatedAt = now;

        db.TicketStatusHistories.Add(new TicketStatusHistory
        {
            TicketId = ticket.Id,
            FromStatus = previous,
            ToStatus = TicketStatus.Assigned,
            ChangedByUserId = adminId,
            ChangedAt = now,
            Note = $"Assigned to {technician.FullName}"
        });

        await SaveChangesSafelyAsync();

        return await GetByIdAsync(id);
    }

    public async Task<TicketDetailResponse> ChangeStatusAsync(
        int id,
        ChangeStatusRequest request,
        string adminId)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            throw new KeyNotFoundException(
                $"Ticket {id} was not found.");
        }

        var current = ticket.Status;
        var target = request.NewStatus;

        if (!TicketWorkflow.CanMove(current, target))
        {
            throw new InvalidOperationException(
                DescribeIllegalMove(current, target));
        }

        if (target == TicketStatus.Assigned)
        {
            throw new InvalidOperationException(
                "A ticket becomes Assigned only when a technician " +
                "is assigned. Use the assign action.");
        }

        var now = DateTime.UtcNow;

        ticket.Status = target;
        ticket.UpdatedAt = now;

        if (target == TicketStatus.Resolved)
        {
            ticket.ResolvedAt = now;
        }

        db.TicketStatusHistories.Add(new TicketStatusHistory
        {
            TicketId = ticket.Id,
            FromStatus = current,
            ToStatus = target,
            ChangedByUserId = adminId,
            ChangedAt = now,
            Note = string.IsNullOrWhiteSpace(request.Note)
                ? null
                : request.Note.Trim()
        });

        await SaveChangesSafelyAsync();

        return await GetByIdAsync(id);
    }

    public async Task<TicketDetailResponse> ConfirmFixAsync(
        int id,
        string reporterId)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            throw new KeyNotFoundException(
                $"Ticket {id} was not found.");
        }

        if (ticket.ReporterId != reporterId)
        {
            throw new UnauthorizedAccessException(
                "Only the person who reported this issue can confirm the fix.");
        }

        if (ticket.Status != TicketStatus.Resolved)
        {
            throw new ArgumentException(
                "You can confirm a fix only after the ticket is Resolved.");
        }

        if (ticket.ConfirmedFixedAt is not null)
        {
            throw new ArgumentException(
                "This fix is already confirmed.");
        }

        var now = DateTime.UtcNow;

        ticket.ConfirmedFixedAt = now;
        ticket.UpdatedAt = now;

        await SaveChangesSafelyAsync();

        return await GetByIdAsync(id);
    }

    // ---------- Helpers ----------

    private async Task SaveChangesSafelyAsync()
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "This ticket was just changed by someone else. " +
                "Please refresh and try again.");
        }
    }

    private static IQueryable<Ticket> ApplyFilters(
        IQueryable<Ticket> query,
        TicketQueryParameters p)
    {
        if (p.BuildingId.HasValue)
        {
            query = query.Where(t =>
                t.BuildingId == p.BuildingId.Value);
        }

        if (p.Status.HasValue)
        {
            query = query.Where(t =>
                t.Status == p.Status.Value);
        }

        if (p.Category.HasValue)
        {
            query = query.Where(t =>
                t.Category == p.Category.Value);
        }

        if (p.Urgency.HasValue)
        {
            query = query.Where(t =>
                t.Urgency == p.Urgency.Value);
        }

        if (p.OpenOnly == true)
        {
            query = query.Where(t =>
                t.Status != TicketStatus.Resolved);
        }

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var pattern = $"%{p.Search.Trim()}%";

            query = query.Where(t =>
                EF.Functions.ILike(t.Description, pattern) ||
                EF.Functions.ILike(t.Room, pattern));
        }

        return query;
    }

    private static async Task<PagedResult<TicketListItemResponse>> ToPagedAsync(
        IQueryable<Ticket> query,
        TicketQueryParameters p)
    {
        var total = await query.CountAsync();

        // OpenOnly = admin dashboard mode:
        // most urgent first, oldest waiting first.
        var ordered = p.OpenOnly == true
            ? query
                .OrderByDescending(UrgencyRank)
                .ThenBy(t => t.CreatedAt)
            : query
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id);

        var page = ordered
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize);

        return new PagedResult<TicketListItemResponse>
        {
            Items = await ToListItemsAsync(page),
            Page = p.Page,
            PageSize = p.PageSize,
            TotalCount = total
        };
    }

    private static async Task<List<TicketListItemResponse>> ToListItemsAsync(
        IQueryable<Ticket> query)
    {
        var rows = await query
            .Select(t => new
            {
                t.Id,
                t.Category,
                BuildingName = t.Building.Name,
                t.Room,
                t.Urgency,
                t.Status,
                ReporterName = t.Reporter.FullName,
                TechnicianName =
                    t.AssignedTechnician != null
                        ? t.AssignedTechnician.FullName
                        : null,
                t.CreatedAt,
                AttachmentCount = t.Attachments.Count
            })
            .ToListAsync();

        return rows
            .Select(r => new TicketListItemResponse(
                r.Id,
                Ticket.BuildReferenceCode(r.Id),
                r.Category,
                r.BuildingName,
                r.Room,
                r.Urgency,
                r.Status,
                r.ReporterName,
                r.TechnicianName,
                r.CreatedAt,
                r.AttachmentCount))
            .ToList();
    }

    private static TicketDetailResponse ToDetailResponse(Ticket t)
    {
        TicketStatus? next =
            TicketWorkflow.TryGetNext(
                t.Status,
                out var n)
                ? n
                : null;

        var history = t.StatusHistory
            .OrderBy(h => h.ChangedAt)
            .ThenBy(h => h.Id)
            .Select(h => new TicketHistoryItemResponse(
                h.FromStatus,
                h.ToStatus,
                h.ChangedByUser.FullName,
                h.ChangedAt,
                h.Note))
            .ToList();

        var attachments = t.Attachments
            .OrderBy(a => a.Id)
            .Select(a => new AttachmentResponse(
                a.Id,
                a.Type,
                a.OriginalFileName,
                a.ContentType,
                a.SizeInBytes,
                a.UploadedByUser.FullName,
                a.CreatedAt,
                $"/api/tickets/{t.Id}/attachments/{a.Id}/file"))
            .ToList();

        return new TicketDetailResponse(
            t.Id,
            t.ReferenceCode,
            t.Category,
            t.BuildingId,
            t.Building.Name,
            t.Room,
            t.Description,
            t.Urgency,
            t.Status,
            next,
            t.Reporter.FullName,
            t.AssignedTechnicianId,
            t.AssignedTechnician?.FullName,
            t.CreatedAt,
            t.AssignedAt,
            t.ResolvedAt,
            t.ConfirmedFixedAt,
            history,
            attachments);
    }

    private static string DescribeIllegalMove(
        TicketStatus current,
        TicketStatus target)
    {
        var move =
            $"Cannot move a ticket from {Display(current)} " +
            $"to {Display(target)}.";

        return TicketWorkflow.TryGetNext(
            current,
            out var next)
            ? $"{move} The only allowed next step is {Display(next)}."
            : $"{move} A {Display(current)} ticket is final " +
              "and cannot change status.";
    }

    private static string Display(TicketStatus status) =>
        status == TicketStatus.InProgress
            ? "In Progress"
            : status.ToString();
}
using FixMyCampus.Application.DTOs.Dashboard;
using FixMyCampus.Application.Interfaces;
using FixMyCampus.Domain.Enums;
using FixMyCampus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixMyCampus.Infrastructure.Services;

public class DashboardService(AppDbContext db) : IDashboardService
{
    private static readonly TimeSpan OverdueAfter = TimeSpan.FromHours(24);

    public async Task<DashboardSummaryResponse> GetSummaryAsync()
    {
        var byStatus = await db.Tickets
            .AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        int CountOf(TicketStatus status) => byStatus.GetValueOrDefault(status);

        var newCount = CountOf(TicketStatus.New);
        var assignedCount = CountOf(TicketStatus.Assigned);
        var inProgressCount = CountOf(TicketStatus.InProgress);
        var resolvedCount = CountOf(TicketStatus.Resolved);

        var cutoff = DateTime.UtcNow - OverdueAfter;
        var overdue = await db.Tickets.CountAsync(t =>
            t.Urgency == TicketUrgency.High &&
            t.Status == TicketStatus.New &&
            t.CreatedAt < cutoff);

        var resolvedTimes = await db.Tickets
            .AsNoTracking()
            .Where(t => t.ResolvedAt != null)
            .Select(t => new { t.CreatedAt, ResolvedAt = t.ResolvedAt!.Value })
            .ToListAsync();

        double? averageHours = resolvedTimes.Count == 0
            ? null
            : Math.Round(resolvedTimes.Average(x => (x.ResolvedAt - x.CreatedAt).TotalHours), 1);

        var open = db.Tickets.AsNoTracking().Where(t => t.Status != TicketStatus.Resolved);

        var byBuilding = await open
            .GroupBy(t => t.Building.Name)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .ToListAsync();

        var byCategory = await open
            .GroupBy(t => t.Category)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .ToListAsync();

        return new DashboardSummaryResponse(
            OpenTickets: newCount + assignedCount + inProgressCount,
            NewTickets: newCount,
            AssignedTickets: assignedCount,
            InProgressTickets: inProgressCount,
            ResolvedTickets: resolvedCount,
            OverdueHighUrgency: overdue,
            AverageResolutionHours: averageHours,
            OpenByBuilding: byBuilding
                .OrderByDescending(x => x.Count)
                .Select(x => new CountByLabelResponse(x.Label, x.Count))
                .ToList(),
            OpenByCategory: byCategory
                .OrderByDescending(x => x.Count)
                .Select(x => new CountByLabelResponse(x.Label.ToString(), x.Count))
                .ToList());
    }
}
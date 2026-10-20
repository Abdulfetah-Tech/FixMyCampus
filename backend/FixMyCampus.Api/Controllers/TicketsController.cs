using System.Security.Claims;
using FixMyCampus.Application.Common;
using FixMyCampus.Application.DTOs.Tickets;
using FixMyCampus.Application.Interfaces;
using FixMyCampus.Domain.Entities;
using FixMyCampus.Domain.Enums;
using FixMyCampus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixMyCampus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController(
    ITicketService ticketService,
    AppDbContext db,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    private const long MaxAttachmentBytes = 25 * 1024 * 1024;
    private static readonly Dictionary<string, AttachmentType> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = AttachmentType.Image,
        ["image/png"] = AttachmentType.Image,
        ["image/webp"] = AttachmentType.Image,
        ["audio/mpeg"] = AttachmentType.Audio,
        ["audio/wav"] = AttachmentType.Audio,
        ["video/mp4"] = AttachmentType.Video,
        ["video/webm"] = AttachmentType.Video
    };

    [HttpPost]
    [Authorize(Roles = "Reporter")]
    public async Task<ActionResult<TicketDetailResponse>> Create([FromBody] CreateTicketRequest request)
    {
        var reporterId = GetCurrentUserId();
        var ticket = await ticketService.CreateAsync(request, reporterId);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Technician")]
    public async Task<ActionResult<PagedResult<TicketListItemResponse>>> GetFeed([FromQuery] TicketQueryParameters query) =>
        Ok(await ticketService.GetFeedAsync(query));

    [HttpGet("mine")]
    [Authorize(Roles = "Reporter")]
    public async Task<ActionResult<PagedResult<TicketListItemResponse>>> GetMine([FromQuery] TicketQueryParameters query) =>
        Ok(await ticketService.GetMineAsync(GetCurrentUserId(), query));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDetailResponse>> GetById(int id) =>
        Ok(await ticketService.GetByIdAsync(id, GetCurrentUserId(), User.IsInRole("Admin") ? "Admin" : User.IsInRole("Technician") ? "Technician" : "Reporter"));

    [HttpGet("similar")]
    [Authorize(Roles = "Reporter")]
    public async Task<ActionResult<IReadOnlyList<TicketListItemResponse>>> FindSimilar([FromQuery] int buildingId, [FromQuery] string room, [FromQuery] TicketCategory category) =>
        Ok(await ticketService.FindSimilarOpenAsync(buildingId, room, category));

    [HttpPost("{id:int}/attachments")]
    [Authorize(Roles = "Reporter,Admin,Technician")]
    [RequestSizeLimit(MaxAttachmentBytes + 1024 * 1024)]
    public async Task<ActionResult<AttachmentResponse>> UploadAttachment(int id, IFormFile file)
    {
        if (file is null || file.Length == 0) return BadRequest("A non-empty file is required.");
        if (file.Length > MaxAttachmentBytes) return BadRequest("Attachments cannot exceed 25 MB.");
        if (!AllowedTypes.TryGetValue(file.ContentType, out var type)) return BadRequest("Unsupported attachment type.");

        var userId = GetCurrentUserId();
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();
        if (!await CanAccessTicketAsync(ticket, userId)) return Forbid();

        var root = configuration["AttachmentStoragePath"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "attachments");
        Directory.CreateDirectory(root);
        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var path = Path.Combine(root, storedName);
        await using (var output = System.IO.File.Create(path)) await file.CopyToAsync(output);

        var attachment = new TicketAttachment
        {
            TicketId = id, Type = type,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedName, ContentType = file.ContentType,
            SizeInBytes = file.Length, UploadedByUserId = userId
        };
        db.TicketAttachments.Add(attachment);
        await db.SaveChangesAsync();
        var name = await db.Users.Where(u => u.Id == userId).Select(u => u.FullName).SingleAsync();
        return CreatedAtAction(nameof(DownloadAttachment), new { id, attachmentId = attachment.Id },
            new AttachmentResponse(attachment.Id, type, attachment.OriginalFileName, attachment.ContentType, attachment.SizeInBytes, name, attachment.CreatedAt, $"/api/tickets/{id}/attachments/{attachment.Id}/file"));
    }

    [HttpGet("{id:int}/attachments/{attachmentId:int}/file")]
    public async Task<IActionResult> DownloadAttachment(int id, int attachmentId)
    {
        var userId = GetCurrentUserId();
        var attachment = await db.TicketAttachments.Include(a => a.Ticket).FirstOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == id);
        if (attachment is null) return NotFound();
        if (!await CanAccessTicketAsync(attachment.Ticket, userId)) return Forbid();
        var root = configuration["AttachmentStoragePath"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "attachments");
        var path = Path.Combine(root, attachment.StoredFileName);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, attachment.ContentType, attachment.OriginalFileName, enableRangeProcessing: true);
    }

    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TicketDetailResponse>> Assign(int id, [FromBody] AssignTicketRequest request) =>
        Ok(await ticketService.AssignAsync(id, request, GetCurrentUserId()));

    [HttpPost("{id:int}/status")]
    [Authorize(Roles = "Admin,Technician")]
    public async Task<ActionResult<TicketDetailResponse>> ChangeStatus(int id, [FromBody] ChangeStatusRequest request) =>
        Ok(await ticketService.ChangeStatusAsync(id, request, GetCurrentUserId(), User.IsInRole("Admin")));

    [HttpPost("{id:int}/confirm-fix")]
    [Authorize(Roles = "Reporter")]
    public async Task<ActionResult<TicketDetailResponse>> ConfirmFix(int id) =>
        Ok(await ticketService.ConfirmFixAsync(id, GetCurrentUserId()));

    private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("Authenticated user ID is missing.");

    private async Task<bool> CanAccessTicketAsync(Ticket ticket, string userId)
    {
        if (User.IsInRole("Admin") || ticket.ReporterId == userId) return true;
        return User.IsInRole("Technician") && await db.Technicians.AnyAsync(t => t.Id == ticket.AssignedTechnicianId && t.UserId == userId && t.IsActive);
    }
}

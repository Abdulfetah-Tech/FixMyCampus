using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Application.DTOs.Tickets;

public record TicketListItemResponse(
    int Id,
    string ReferenceCode,
    TicketCategory Category,
    string BuildingName,
    string Room,
    TicketUrgency Urgency,
    TicketStatus Status,
    string ReporterName,
    string? AssignedTechnicianName,
    DateTime CreatedAt,
    int AttachmentCount);
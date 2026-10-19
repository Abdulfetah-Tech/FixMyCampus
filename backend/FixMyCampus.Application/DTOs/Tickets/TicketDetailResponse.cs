using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Application.DTOs.Tickets;

public record TicketDetailResponse(
    int Id,
    string ReferenceCode,
    TicketCategory Category,
    int BuildingId,
    string BuildingName,
    string Room,
    string Description,
    TicketUrgency Urgency,
    TicketStatus Status,
    TicketStatus? NextAllowedStatus,
    string ReporterName,
    int? AssignedTechnicianId,
    string? AssignedTechnicianName,
    DateTime CreatedAt,
    DateTime? AssignedAt,
    DateTime? ResolvedAt,
    DateTime? ConfirmedFixedAt,
    IReadOnlyList<TicketHistoryItemResponse> History,
    IReadOnlyList<AttachmentResponse> Attachments);
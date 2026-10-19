using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Application.DTOs.Tickets;

public record TicketHistoryItemResponse(
    TicketStatus? FromStatus,
    TicketStatus ToStatus,
    string ChangedByName,
    DateTime ChangedAt,
    string? Note);
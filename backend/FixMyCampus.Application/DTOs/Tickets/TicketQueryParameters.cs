using System.ComponentModel.DataAnnotations;
using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Application.DTOs.Tickets;

public class TicketQueryParameters
{
    public int? BuildingId { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketCategory? Category { get; set; }
    public TicketUrgency? Urgency { get; set; }
    public bool? OpenOnly { get; set; }
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;
}
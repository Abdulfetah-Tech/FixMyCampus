using System.ComponentModel.DataAnnotations;
using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Application.DTOs.Tickets;

public class CreateTicketRequest
{
    [EnumDataType(typeof(TicketCategory))]
    public TicketCategory Category { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a building.")]
    public int BuildingId { get; set; }

    [Required, StringLength(50)]
    public string Room { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [EnumDataType(typeof(TicketUrgency))]
    public TicketUrgency Urgency { get; set; } = TicketUrgency.Medium;
}
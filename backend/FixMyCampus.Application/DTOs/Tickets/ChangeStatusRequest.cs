using System.ComponentModel.DataAnnotations;
using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Application.DTOs.Tickets;

public class ChangeStatusRequest
{
    [EnumDataType(typeof(TicketStatus))]
    public TicketStatus NewStatus { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
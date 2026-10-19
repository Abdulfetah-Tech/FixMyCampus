using System.ComponentModel.DataAnnotations;

namespace FixMyCampus.Application.DTOs.Tickets;

public class AssignTicketRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Please select a technician.")]
    public int TechnicianId { get; set; }
}
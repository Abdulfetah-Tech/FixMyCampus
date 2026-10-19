using FixMyCampus.Domain.Common;
using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Domain.Entities;

public class Ticket : BaseEntity
{    public static string BuildReferenceCode(int id) => $"FMC-{id:D5}";
    public string ReferenceCode => BuildReferenceCode(Id);
    public TicketCategory Category { get; set; }
    public int BuildingId { get; set; }
    public Building Building { get; set; } = null!;
    public string Room { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketUrgency Urgency { get; set; } = TicketUrgency.Medium;
    public TicketStatus Status { get; set; } = TicketStatus.New;

    public string ReporterId { get; set; } = string.Empty;
    public AppUser Reporter { get; set; } = null!;

    public int? AssignedTechnicianId { get; set; }
    public Technician? AssignedTechnician { get; set; }

    public DateTime? AssignedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ConfirmedFixedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TicketStatusHistory> StatusHistory { get; set; } = new List<TicketStatusHistory>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
}
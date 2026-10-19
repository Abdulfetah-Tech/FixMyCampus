using FixMyCampus.Domain.Common;

namespace FixMyCampus.Domain.Entities;

public class Technician : BaseEntity
{
    
        public string? UserId { get; set; }
    public AppUser? User { get; set; }
    
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
}
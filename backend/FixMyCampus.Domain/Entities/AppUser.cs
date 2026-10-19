using Microsoft.AspNetCore.Identity;
using FixMyCampus.Domain.Enums;
namespace FixMyCampus.Domain.Entities;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? UniversityId { get; set; }
    public RegistrationRole RequestedRole { get; set; } = RegistrationRole.Reporter;
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByUserId { get; set; }
    public string? RejectionReason { get; set; }
    public ICollection<Ticket> ReportedTickets { get; set; } = new List<Ticket>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
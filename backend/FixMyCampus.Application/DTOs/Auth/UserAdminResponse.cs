using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Application.DTOs.Auth;

public record UserAdminResponse(
    string Id,
    string FullName,
    string Email,
    string? UniversityId,
    RegistrationRole RequestedRole,
    string? AssignedRole,
    ApprovalStatus ApprovalStatus,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? RejectionReason);
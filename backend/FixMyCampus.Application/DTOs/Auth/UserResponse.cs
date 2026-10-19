using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Application.DTOs.Auth;

public record UserResponse(
    string Id,
    string FullName,
    string Email,
    string Role,
    ApprovalStatus ApprovalStatus);
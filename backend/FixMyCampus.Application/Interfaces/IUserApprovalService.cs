using FixMyCampus.Application.DTOs.Auth;
using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Application.Interfaces;

public interface IUserApprovalService
{
    Task<IReadOnlyList<UserAdminResponse>> GetUsersAsync(
        ApprovalStatus? status);

    Task<UserAdminResponse> ApproveAsync(
        string userId,
        string adminId,
        RegistrationRole? assignedRole);

    Task<UserAdminResponse> RejectAsync(
        string userId,
        string adminId,
        string reason);
}
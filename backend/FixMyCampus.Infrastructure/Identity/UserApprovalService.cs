using FixMyCampus.Application.DTOs.Auth;
using FixMyCampus.Application.Interfaces;
using FixMyCampus.Domain.Common;
using FixMyCampus.Domain.Entities;
using FixMyCampus.Domain.Enums;
using FixMyCampus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FixMyCampus.Infrastructure.Identity;

public class UserApprovalService(
    AppDbContext db,
    UserManager<AppUser> userManager) : IUserApprovalService
{
    public async Task<IReadOnlyList<UserAdminResponse>> GetUsersAsync(
        ApprovalStatus? status)
    {
        var query = db.Users
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(
                u => u.ApprovalStatus == status.Value);
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var ids = users
            .Select(u => u.Id)
            .ToList();

        var roleRows = await (
            from ur in db.UserRoles
            join r in db.Roles
                on ur.RoleId equals r.Id
            where ids.Contains(ur.UserId)
            select new
            {
                ur.UserId,
                RoleName = r.Name
            })
            .ToListAsync();

        var roleByUser = roleRows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.First().RoleName);

        return users
            .Select(u =>
                ToResponse(
                    u,
                    roleByUser.GetValueOrDefault(u.Id)))
            .ToList();
    }

    public async Task<UserAdminResponse> ApproveAsync(
        string userId,
        string adminId,
        RegistrationRole? assignedRole)
    {
        var user = await GetPendingUserAsync(userId);

        var roleName = ToRoleName(
            assignedRole ?? user.RequestedRole);

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var roleResult =
            await userManager.AddToRoleAsync(user, roleName);

        if (!roleResult.Succeeded)
        {
            throw new ArgumentException(
                string.Join(
                    " ",
                    roleResult.Errors.Select(e => e.Description)));
        }

        user.ApprovalStatus = ApprovalStatus.Approved;
        user.ReviewedAt = DateTime.UtcNow;
        user.ReviewedByUserId = adminId;
        user.RejectionReason = null;

        var updateResult =
            await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new ArgumentException(
                string.Join(
                    " ",
                    updateResult.Errors.Select(e => e.Description)));
        }

        // An approved technician becomes assignable by name.
        if (roleName == Roles.Technician &&
            !await db.Technicians.AnyAsync(
                t => t.UserId == user.Id))
        {
            db.Technicians.Add(
                new Technician
                {
                    FullName = user.FullName,
                    UserId = user.Id
                });

            await db.SaveChangesAsync();
        }

        await transaction.CommitAsync();

        return ToResponse(user, roleName);
    }

    public async Task<UserAdminResponse> RejectAsync(
        string userId,
        string adminId,
        string reason)
    {
        var user = await GetPendingUserAsync(userId);

        user.ApprovalStatus = ApprovalStatus.Rejected;
        user.ReviewedAt = DateTime.UtcNow;
        user.ReviewedByUserId = adminId;
        user.RejectionReason = reason.Trim();

        var result =
            await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new ArgumentException(
                string.Join(
                    " ",
                    result.Errors.Select(e => e.Description)));
        }

        return ToResponse(user, null);
    }

    private async Task<AppUser> GetPendingUserAsync(
        string userId)
    {
        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "User not found.");
        }

        if (user.ApprovalStatus != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only accounts that are waiting for review " +
                "can be approved or rejected.");
        }

        return user;
    }

    private static string ToRoleName(
        RegistrationRole role) =>
        role switch
        {
            RegistrationRole.Reporter =>
                Roles.Reporter,

            RegistrationRole.Technician =>
                Roles.Technician,

            _ => throw new ArgumentException(
                "Invalid role.")
        };

    private static UserAdminResponse ToResponse(
        AppUser user,
        string? assignedRole) =>
        new(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            user.UniversityId,
            user.RequestedRole,
            assignedRole,
            user.ApprovalStatus,
            user.CreatedAt,
            user.ReviewedAt,
            user.RejectionReason);
}
using FixMyCampus.Application.Common;
using FixMyCampus.Application.DTOs.Auth;
using FixMyCampus.Application.Interfaces;
using FixMyCampus.Domain.Entities;
using FixMyCampus.Domain.Enums;
using FixMyCampus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FixMyCampus.Infrastructure.Identity;

public class AuthService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    ITokenService tokenService,
    IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<UserResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim();
        var universityId = request.UniversityId.Trim();

        if (await userManager.FindByEmailAsync(email) is not null)
            throw new InvalidOperationException(
                "An account with this email already exists.");

        if (await db.Users.AnyAsync(u => u.UniversityId == universityId))
            throw new InvalidOperationException(
                "This university ID is already registered.");

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim(),
            UniversityId = universityId,
            RequestedRole = request.RequestedRole,
            ApprovalStatus = ApprovalStatus.Pending
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            throw new ArgumentException(
                string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        // No Identity role yet:
        // the admin assigns it when approving the account.
        return ToUserResponse(user, string.Empty);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim())
            ?? throw new UnauthorizedAccessException(
                "Invalid email or password.");

        if (await userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedAccessException(
                "Too many failed attempts. Please try again in a few minutes.");
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);

            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        await userManager.ResetAccessFailedCountAsync(user);

        // Approval is checked only AFTER the password is correct,
        // so strangers cannot probe which emails are registered.
        EnsureApproved(user);

        return await IssueTokensAsync(user, replacing: null);
    }

    public async Task<AuthResult> RefreshAsync(string? rawRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            throw new UnauthorizedAccessException(
                "Session expired. Please sign in again.");
        }

        var hash = tokenService.HashToken(rawRefreshToken);

        var stored = await db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == hash);

        if (stored is null)
        {
            throw new UnauthorizedAccessException(
                "Session expired. Please sign in again.");
        }

        if (stored.IsRevoked)
        {
            // An already-used token came back:
            // possible theft. End every session of this user.
            await RevokeAllForUserAsync(stored.UserId);

            throw new UnauthorizedAccessException(
                "Session is no longer valid. Please sign in again.");
        }

        if (stored.IsExpired)
        {
            throw new UnauthorizedAccessException(
                "Session expired. Please sign in again.");
        }

        EnsureApproved(stored.User);

        return await IssueTokensAsync(stored.User, replacing: stored);
    }

    public async Task LogoutAsync(string? rawRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            return;

        var hash = tokenService.HashToken(rawRefreshToken);

        var stored = await db.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == hash);

        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task<UserResponse> GetCurrentUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new UnauthorizedAccessException(
                "Session expired. Please sign in again.");

        EnsureApproved(user);

        var role = (await userManager.GetRolesAsync(user))
            .FirstOrDefault() ?? string.Empty;

        return ToUserResponse(user, role);
    }

    private async Task<AuthResult> IssueTokensAsync(
        AppUser user,
        RefreshToken? replacing)
    {
        var role = (await userManager.GetRolesAsync(user))
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new InvalidOperationException(
                "No role is assigned to this account yet.");
        }

        var accessToken = tokenService.CreateAccessToken(user, role);

        var accessExpiresAt =
            DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

        var refresh = tokenService.CreateRefreshToken();

        if (replacing is not null)
        {
            replacing.RevokedAt = DateTime.UtcNow;
            replacing.ReplacedByTokenHash = refresh.TokenHash;
        }

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refresh.TokenHash,
            ExpiresAt = refresh.ExpiresAt
        });

        await db.SaveChangesAsync();

        return new AuthResult(
            ToUserResponse(user, role),
            accessToken,
            accessExpiresAt,
            refresh.RawToken,
            refresh.ExpiresAt);
    }

    private Task RevokeAllForUserAsync(string userId) =>
        db.RefreshTokens
            .Where(r =>
                r.UserId == userId &&
                r.RevokedAt == null)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(
                    r => r.RevokedAt,
                    (DateTime?)DateTime.UtcNow));

    private static void EnsureApproved(AppUser user)
    {
        switch (user.ApprovalStatus)
        {
            case ApprovalStatus.Pending:
                throw new InvalidOperationException(
                    "Your account is waiting for admin approval.");

            case ApprovalStatus.Rejected:
                throw new InvalidOperationException(
                    $"Your registration was rejected. " +
                    $"Reason: {user.RejectionReason}");
        }
    }

    private static UserResponse ToUserResponse(
        AppUser user,
        string role) =>
        new(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            role,
            user.ApprovalStatus);
}
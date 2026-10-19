using FixMyCampus.Application.DTOs.Auth;

namespace FixMyCampus.Application.Interfaces;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RefreshAsync(string? rawRefreshToken);
    Task LogoutAsync(string? rawRefreshToken);
    Task<UserResponse> GetCurrentUserAsync(string userId);
}
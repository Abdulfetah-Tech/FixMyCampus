namespace FixMyCampus.Application.DTOs.Auth;

// Internal hand-off from AuthService to the controller. Never serialized to the client:
// the tokens travel only in HttpOnly cookies.
public record AuthResult(
    UserResponse User,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
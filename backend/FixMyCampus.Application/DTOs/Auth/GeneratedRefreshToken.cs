namespace FixMyCampus.Application.DTOs.Auth;

public record GeneratedRefreshToken(string RawToken, string TokenHash, DateTime ExpiresAt);
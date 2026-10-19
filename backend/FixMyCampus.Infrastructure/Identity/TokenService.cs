using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FixMyCampus.Application.Common;
using FixMyCampus.Application.DTOs.Auth;
using FixMyCampus.Application.Interfaces;
using FixMyCampus.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FixMyCampus.Infrastructure.Identity;

public class TokenService(
    IOptions<JwtSettings> jwtOptions) : ITokenService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public string CreateAccessToken(AppUser user, string role)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwt.Secret));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id),

            new(
                ClaimTypes.Email,
                user.Email ?? string.Empty),

            new(
                ClaimTypes.Name,
                user.FullName),

            new(
                ClaimTypes.Role,
                role)
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                _jwt.AccessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    public GeneratedRefreshToken CreateRefreshToken()
    {
        var rawToken = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        var tokenHash = HashToken(rawToken);

        var expiresAt = DateTime.UtcNow.AddDays(
            _jwt.RefreshTokenDays);

        return new GeneratedRefreshToken(
            rawToken,
            tokenHash,
            expiresAt);
    }

    public string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(rawToken));

        return Convert.ToBase64String(bytes);
    }
}
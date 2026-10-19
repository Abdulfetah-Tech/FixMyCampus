using FixMyCampus.Application.DTOs.Auth;
using FixMyCampus.Domain.Entities;

namespace FixMyCampus.Application.Interfaces;

public interface ITokenService
{
    string CreateAccessToken(AppUser user, string role);
    GeneratedRefreshToken CreateRefreshToken();
    string HashToken(string rawToken);
}
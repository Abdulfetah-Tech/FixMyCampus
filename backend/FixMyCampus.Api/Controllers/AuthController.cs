using System.Security.Claims;
using FixMyCampus.Application.DTOs.Auth;
using FixMyCampus.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixMyCampus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthService authService,
    IConfiguration configuration) : ControllerBase
{
    private const string AccessTokenCookie = "access_token";
    private const string RefreshTokenCookie = "refresh_token";

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponse>> Register(
        [FromBody] RegisterRequest request)
    {
        var user = await authService.RegisterAsync(request);

        return Ok(user);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponse>> Login(
        [FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);

        SetAuthCookies(result);

        return Ok(result.User);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponse>> Refresh()
    {
        Request.Cookies.TryGetValue(
            RefreshTokenCookie,
            out var refreshToken);

        var result = await authService.RefreshAsync(refreshToken);

        SetAuthCookies(result);

        return Ok(result.User);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        Request.Cookies.TryGetValue(
            RefreshTokenCookie,
            out var refreshToken);

        await authService.LogoutAsync(refreshToken);

        DeleteAuthCookies();

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var user = await authService.GetCurrentUserAsync(userId);

        return Ok(user);
    }

    private void SetAuthCookies(AuthResult result)
    {
        var settings = configuration.GetSection("AuthCookies");

        var secure = settings.GetValue<bool>("Secure");
        var sameSiteValue = settings["SameSite"] ?? "Strict";

        var sameSite = Enum.TryParse<SameSiteMode>(
            sameSiteValue,
            ignoreCase: true,
            out var parsedSameSite)
            ? parsedSameSite
            : SameSiteMode.Strict;

        var accessOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Expires = result.AccessTokenExpiresAt,
            Path = "/"
        };

        var refreshOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Expires = result.RefreshTokenExpiresAt,
            Path = "/api/auth"
        };

        Response.Cookies.Append(
            AccessTokenCookie,
            result.AccessToken,
            accessOptions);

        Response.Cookies.Append(
            RefreshTokenCookie,
            result.RefreshToken,
            refreshOptions);
    }

    private void DeleteAuthCookies()
    {
        Response.Cookies.Delete(
            AccessTokenCookie,
            new CookieOptions
            {
                Path = "/"
            });

        Response.Cookies.Delete(
            RefreshTokenCookie,
            new CookieOptions
            {
                Path = "/api/auth"
            });
    }
}
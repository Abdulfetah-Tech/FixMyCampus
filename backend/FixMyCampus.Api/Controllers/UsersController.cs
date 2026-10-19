using System.Security.Claims;
using FixMyCampus.Application.DTOs.Auth;
using FixMyCampus.Application.Interfaces;
using FixMyCampus.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixMyCampus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController(
    IUserApprovalService userApprovalService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserAdminResponse>>> GetUsers(
        [FromQuery] ApprovalStatus? status)
    {
        var users = await userApprovalService.GetUsersAsync(status);

        return Ok(users);
    }

    [HttpPost("{userId}/approve")]
    public async Task<ActionResult<UserAdminResponse>> Approve(
        string userId,
        [FromBody] ApproveUserRequest request)
    {
        var adminId = GetCurrentUserId();

        var user = await userApprovalService.ApproveAsync(
            userId,
            adminId,
            request.Role);

        return Ok(user);
    }

    [HttpPost("{userId}/reject")]
    public async Task<ActionResult<UserAdminResponse>> Reject(
        string userId,
        [FromBody] RejectUserRequest request)
    {
        var adminId = GetCurrentUserId();

        var user = await userApprovalService.RejectAsync(
            userId,
            adminId,
            request.Reason);

        return Ok(user);
    }

    private string GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException(
                "The authenticated user ID is missing.");

        return userId;
    }
}
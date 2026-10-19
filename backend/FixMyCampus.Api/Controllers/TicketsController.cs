using System.Security.Claims;
using FixMyCampus.Application.Common;
using FixMyCampus.Application.DTOs.Tickets;
using FixMyCampus.Application.Interfaces;
using FixMyCampus.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixMyCampus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController(
    ITicketService ticketService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Reporter")]
    public async Task<ActionResult<TicketDetailResponse>> Create(
        [FromBody] CreateTicketRequest request)
    {
        var reporterId = GetCurrentUserId();

        var ticket = await ticketService.CreateAsync(
            request,
            reporterId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = ticket.Id },
            ticket);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Technician")]
    public async Task<ActionResult<PagedResult<TicketListItemResponse>>> GetFeed(
        [FromQuery] TicketQueryParameters query)
    {
        var result = await ticketService.GetFeedAsync(query);

        return Ok(result);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Reporter")]
    public async Task<ActionResult<PagedResult<TicketListItemResponse>>> GetMine(
        [FromQuery] TicketQueryParameters query)
    {
        var reporterId = GetCurrentUserId();

        var result = await ticketService.GetMineAsync(
            reporterId,
            query);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDetailResponse>> GetById(
        int id)
    {
        var ticket = await ticketService.GetByIdAsync(id);

        return Ok(ticket);
    }

    [HttpGet("similar")]
    [Authorize(Roles = "Reporter")]
    public async Task<ActionResult<IReadOnlyList<TicketListItemResponse>>> FindSimilar(
        [FromQuery] int buildingId,
        [FromQuery] string room,
        [FromQuery] TicketCategory category)
    {
        var tickets = await ticketService.FindSimilarOpenAsync(
            buildingId,
            room,
            category);

        return Ok(tickets);
    }

    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TicketDetailResponse>> Assign(
        int id,
        [FromBody] AssignTicketRequest request)
    {
        var adminId = GetCurrentUserId();

        var ticket = await ticketService.AssignAsync(
            id,
            request,
            adminId);

        return Ok(ticket);
    }

    [HttpPost("{id:int}/status")]
    [Authorize(Roles = "Admin,Technician")]
    public async Task<ActionResult<TicketDetailResponse>> ChangeStatus(
        int id,
        [FromBody] ChangeStatusRequest request)
    {
        var userId = GetCurrentUserId();

        var ticket = await ticketService.ChangeStatusAsync(
            id,
            request,
            userId);

        return Ok(ticket);
    }

    [HttpPost("{id:int}/confirm-fix")]
    [Authorize(Roles = "Reporter")]
    public async Task<ActionResult<TicketDetailResponse>> ConfirmFix(
        int id)
    {
        var reporterId = GetCurrentUserId();

        var ticket = await ticketService.ConfirmFixAsync(
            id,
            reporterId);

        return Ok(ticket);
    }

    private string GetCurrentUserId()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException(
                "Authenticated user ID is missing.");

        return userId;
    }
}
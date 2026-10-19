using FixMyCampus.Application.DTOs.Lookups;
using FixMyCampus.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixMyCampus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LookupsController(
    ILookupService lookupService) : ControllerBase
{
    [HttpGet("buildings")]
    public async Task<ActionResult<IReadOnlyList<BuildingResponse>>> GetBuildings()
    {
        var buildings = await lookupService.GetBuildingsAsync();

        return Ok(buildings);
    }

    [HttpGet("technicians")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<TechnicianResponse>>> GetTechnicians()
    {
        var technicians = await lookupService.GetTechniciansAsync();

        return Ok(technicians);
    }
}
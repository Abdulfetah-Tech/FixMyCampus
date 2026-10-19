using FixMyCampus.Application.DTOs.Lookups;
using FixMyCampus.Application.Interfaces;
using FixMyCampus.Domain.Enums;
using FixMyCampus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixMyCampus.Infrastructure.Services;

public class LookupService(AppDbContext db) : ILookupService
{
    public async Task<IReadOnlyList<BuildingResponse>> GetBuildingsAsync() =>
        await db.Buildings
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BuildingResponse(b.Id, b.Name, b.Code))
            .ToListAsync();

    public async Task<IReadOnlyList<TechnicianResponse>> GetTechniciansAsync() =>
        await db.Technicians
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.FullName)
            .Select(t => new TechnicianResponse(
                t.Id,
                t.FullName,
                t.AssignedTickets.Count(x =>
                    x.Status == TicketStatus.Assigned || x.Status == TicketStatus.InProgress)))
            .ToListAsync();
}
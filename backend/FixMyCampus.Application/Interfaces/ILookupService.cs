using FixMyCampus.Application.DTOs.Lookups;

namespace FixMyCampus.Application.Interfaces;

public interface ILookupService
{
    Task<IReadOnlyList<BuildingResponse>> GetBuildingsAsync();
    Task<IReadOnlyList<TechnicianResponse>> GetTechniciansAsync();
}
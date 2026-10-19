using FixMyCampus.Application.Common;
using FixMyCampus.Application.DTOs.Tickets;
using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Application.Interfaces;

public interface ITicketService
{
    Task<TicketDetailResponse> CreateAsync(CreateTicketRequest request, string reporterId);
    Task<PagedResult<TicketListItemResponse>> GetFeedAsync(TicketQueryParameters query);
    Task<PagedResult<TicketListItemResponse>> GetMineAsync(string reporterId, TicketQueryParameters query);
    Task<TicketDetailResponse> GetByIdAsync(int id);
    Task<TicketDetailResponse> AssignAsync(int id, AssignTicketRequest request, string adminId);
    Task<TicketDetailResponse> ChangeStatusAsync(int id, ChangeStatusRequest request, string adminId);
    Task<TicketDetailResponse> ConfirmFixAsync(int id, string reporterId);
    Task<IReadOnlyList<TicketListItemResponse>> FindSimilarOpenAsync(int buildingId, string room, TicketCategory category);
}
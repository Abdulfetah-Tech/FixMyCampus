namespace FixMyCampus.Application.DTOs.Dashboard;

public record DashboardSummaryResponse(
    int OpenTickets,
    int NewTickets,
    int AssignedTickets,
    int InProgressTickets,
    int ResolvedTickets,
    int OverdueHighUrgency,
    double? AverageResolutionHours,
    IReadOnlyList<CountByLabelResponse> OpenByBuilding,
    IReadOnlyList<CountByLabelResponse> OpenByCategory);
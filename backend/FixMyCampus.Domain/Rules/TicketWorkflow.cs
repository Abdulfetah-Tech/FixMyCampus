using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Domain.Rules;

public static class TicketWorkflow
{
    private static readonly Dictionary<TicketStatus, TicketStatus> NextStatus = new()
    {
        [TicketStatus.New] = TicketStatus.Assigned,
        [TicketStatus.Assigned] = TicketStatus.InProgress,
        [TicketStatus.InProgress] = TicketStatus.Resolved
    };

    public static bool TryGetNext(TicketStatus current, out TicketStatus next)
        => NextStatus.TryGetValue(current, out next);

    public static bool CanMove(TicketStatus current, TicketStatus target)
        => NextStatus.TryGetValue(current, out var next) && next == target;
}
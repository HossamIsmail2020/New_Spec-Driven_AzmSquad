using CustomerSupportCRM.Domain.Enums;

namespace CustomerSupportCRM.Domain.Entities;

/// <summary>Transition table and assignment rule match plan.md §4 exactly (FR-010, FR-012) -- change the table there first if this ever needs to change.</summary>
public class Ticket
{
    private static readonly Dictionary<TicketStatus, TicketStatus[]> AllowedTransitions = new()
    {
        [TicketStatus.Open] = [TicketStatus.InProgress, TicketStatus.Waiting, TicketStatus.Closed],
        [TicketStatus.InProgress] = [TicketStatus.Waiting, TicketStatus.Resolved, TicketStatus.Closed],
        [TicketStatus.Waiting] = [TicketStatus.InProgress, TicketStatus.Resolved, TicketStatus.Closed],
        [TicketStatus.Resolved] = [TicketStatus.Closed, TicketStatus.Open],
        [TicketStatus.Closed] = [TicketStatus.Open]
    };

    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public TicketCategory Category { get; set; }
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; }
    public string? AssignedAgentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<TicketHistoryEntry> History { get; set; } = new List<TicketHistoryEntry>();

    public static bool IsValidTransition(TicketStatus from, TicketStatus to) =>
        from == to || (AllowedTransitions.TryGetValue(from, out var next) && next.Contains(to));

    public void TransitionTo(TicketStatus newStatus)
    {
        if (!IsValidTransition(Status, newStatus))
        {
            throw new InvalidOperationException($"Cannot transition ticket from '{Status}' to '{newStatus}'.");
        }

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanBeAssignedTo(string targetAgentId, UserRole actingUserRole, string actingUserId)
    {
        if (actingUserRole == UserRole.Administrator)
        {
            return true;
        }

        return targetAgentId == actingUserId && AssignedAgentId is null;
    }
}

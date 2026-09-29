using CustomerSupportCRM.Domain.Enums;

namespace CustomerSupportCRM.Domain.Entities;

/// <summary>FR-011: an immutable record of something that happened to a Ticket.</summary>
public class TicketHistoryEntry
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public TicketHistoryEntryType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

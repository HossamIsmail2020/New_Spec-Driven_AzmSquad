using CustomerSupportCRM.Application.Common;
using CustomerSupportCRM.Application.Tickets;
using CustomerSupportCRM.Domain.Entities;
using CustomerSupportCRM.Domain.Enums;
using CustomerSupportCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCRM.Infrastructure.Services;

/// <summary>FR-006..FR-012. Ordering in `Projected` happens on IQueryable&lt;Ticket&gt; BEFORE the Select projection -- EF Core cannot translate an OrderBy applied after a Select containing a correlated subquery (the agent-name lookup below). Confirmed by the prior session's InvalidOperationException.</summary>
public class TicketService(AppDbContext db, ICurrentUserService currentUser) : ITicketService
{
    public async Task<IReadOnlyList<TicketDto>> GetAllAsync(CancellationToken ct = default) =>
        await Projected(db.Tickets.OrderByDescending(t => t.CreatedAt)).ToListAsync(ct);

    public async Task<IReadOnlyList<TicketDto>> GetAssignedToAsync(string agentId, CancellationToken ct = default) =>
        await Projected(db.Tickets
                .Where(t => t.AssignedAgentId == agentId)
                .OrderByDescending(t => t.CreatedAt))
            .ToListAsync(ct);

    public async Task<Result<TicketDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await Projected(db.Tickets.Where(t => t.Id == id)).FirstOrDefaultAsync(ct);
        return dto is null
            ? Result<TicketDto>.Failure(ServiceError.NotFound($"Ticket '{id}' was not found."))
            : Result<TicketDto>.Success(dto);
    }

    public async Task<Result<TicketDto>> CreateAsync(CreateTicketRequest request, CancellationToken ct = default)
    {
        var validationError = ValidateSubjectAndDescription(request.Subject, request.Description);
        if (validationError is not null)
        {
            return Result<TicketDto>.Failure(validationError);
        }

        if (!await db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct))
        {
            return Result<TicketDto>.Failure(ServiceError.Validation(
                "Validation failed.",
                new Dictionary<string, string[]> { ["customerId"] = ["Customer does not exist."] }));
        }

        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim(),
            CustomerId = request.CustomerId,
            Category = request.Category,
            Priority = request.Priority,
            Status = TicketStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Tickets.Add(ticket);
        db.TicketHistoryEntries.Add(NewHistoryEntry(ticket.Id, TicketHistoryEntryType.Created, $"Ticket created with priority {request.Priority}.", now));
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(ticket.Id, ct);
    }

    public async Task<Result<TicketDto>> UpdateAsync(Guid id, UpdateTicketRequest request, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
        {
            return Result<TicketDto>.Failure(ServiceError.NotFound($"Ticket '{id}' was not found."));
        }

        var validationError = ValidateSubjectAndDescription(request.Subject, request.Description);
        if (validationError is not null)
        {
            return Result<TicketDto>.Failure(validationError);
        }

        ticket.Subject = request.Subject.Trim();
        ticket.Description = request.Description.Trim();
        ticket.Category = request.Category;
        ticket.Priority = request.Priority;
        ticket.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<Result<TicketDto>> ChangeStatusAsync(Guid id, ChangeTicketStatusRequest request, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
        {
            return Result<TicketDto>.Failure(ServiceError.NotFound($"Ticket '{id}' was not found."));
        }

        if (!Ticket.IsValidTransition(ticket.Status, request.NewStatus))
        {
            return Result<TicketDto>.Failure(ServiceError.Conflict(
                $"Cannot change status from '{ticket.Status}' to '{request.NewStatus}'."));
        }

        var previousStatus = ticket.Status;
        ticket.TransitionTo(request.NewStatus);

        db.TicketHistoryEntries.Add(NewHistoryEntry(
            ticket.Id, TicketHistoryEntryType.StatusChanged,
            $"Status changed from {previousStatus} to {request.NewStatus}.", ticket.UpdatedAt));

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<Result<TicketDto>> AssignAsync(Guid id, AssignTicketRequest request, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
        {
            return Result<TicketDto>.Failure(ServiceError.NotFound($"Ticket '{id}' was not found."));
        }

        if (!await db.Users.AnyAsync(u => u.Id == request.AgentId, ct))
        {
            return Result<TicketDto>.Failure(ServiceError.Validation(
                "Validation failed.",
                new Dictionary<string, string[]> { ["agentId"] = ["Agent does not exist."] }));
        }

        if (!ticket.CanBeAssignedTo(request.AgentId, currentUser.Role, currentUser.UserId))
        {
            return Result<TicketDto>.Failure(ServiceError.Forbidden(
                "Agents may only self-assign a ticket that is currently unassigned."));
        }

        var previousAgentId = ticket.AssignedAgentId;
        ticket.AssignedAgentId = request.AgentId;
        ticket.UpdatedAt = DateTime.UtcNow;

        db.TicketHistoryEntries.Add(NewHistoryEntry(
            ticket.Id, TicketHistoryEntryType.Assigned,
            previousAgentId is null ? "Ticket assigned." : "Ticket reassigned.", ticket.UpdatedAt));

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<Result<IReadOnlyList<TicketHistoryEntryDto>>> GetHistoryAsync(Guid id, CancellationToken ct = default)
    {
        if (!await db.Tickets.AnyAsync(t => t.Id == id, ct))
        {
            return Result<IReadOnlyList<TicketHistoryEntryDto>>.Failure(ServiceError.NotFound($"Ticket '{id}' was not found."));
        }

        var entries = await db.TicketHistoryEntries
            .Where(h => h.TicketId == id)
            .OrderBy(h => h.CreatedAt)
            .Select(h => new TicketHistoryEntryDto(
                h.Id,
                h.Type.ToString(),
                h.Description,
                h.ActorUserId,
                db.Users.Where(u => u.Id == h.ActorUserId).Select(u => u.FullName).FirstOrDefault() ?? "Unknown",
                h.CreatedAt))
            .ToListAsync(ct);

        return Result<IReadOnlyList<TicketHistoryEntryDto>>.Success(entries);
    }

    private TicketHistoryEntry NewHistoryEntry(Guid ticketId, TicketHistoryEntryType type, string description, DateTime at) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        Type = type,
        Description = description,
        ActorUserId = currentUser.UserId,
        CreatedAt = at
    };

    private static ServiceError? ValidateSubjectAndDescription(string subject, string description)
    {
        var fields = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(subject) || subject.Trim().Length is < 3 or > 200)
        {
            fields["subject"] = ["Subject is required and must be between 3 and 200 characters."];
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            fields["description"] = ["Description is required."];
        }

        return fields.Count == 0 ? null : ServiceError.Validation("Validation failed.", fields);
    }

    private IQueryable<TicketDto> Projected(IQueryable<Ticket> query) => query.Select(t => new TicketDto(
        t.Id,
        t.Subject,
        t.Description,
        t.CustomerId,
        t.Customer!.FullName,
        t.Customer.Email,
        t.Category,
        t.Priority,
        t.Status,
        t.AssignedAgentId,
        t.AssignedAgentId == null
            ? null
            : db.Users.Where(u => u.Id == t.AssignedAgentId).Select(u => u.FullName).FirstOrDefault(),
        t.CreatedAt,
        t.UpdatedAt));
}

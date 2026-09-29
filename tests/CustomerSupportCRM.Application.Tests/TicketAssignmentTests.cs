using CustomerSupportCRM.Domain.Entities;
using CustomerSupportCRM.Domain.Enums;
using Xunit;

namespace CustomerSupportCRM.Application.Tests;

/// <summary>T005 (spec FR-012). Written before Ticket exists -- expected to fail to compile until T006 lands.</summary>
public class TicketAssignmentTests
{
    [Fact]
    public void Administrator_can_assign_a_ticket_to_any_agent_regardless_of_current_assignment()
    {
        var ticket = new Ticket { AssignedAgentId = "agent-1" };

        Assert.True(ticket.CanBeAssignedTo("agent-2", UserRole.Administrator, "admin-1"));
    }

    [Fact]
    public void Agent_can_self_assign_an_unassigned_ticket()
    {
        var ticket = new Ticket { AssignedAgentId = null };

        Assert.True(ticket.CanBeAssignedTo("agent-1", UserRole.Agent, "agent-1"));
    }

    [Fact]
    public void Agent_cannot_assign_an_unassigned_ticket_to_a_different_agent()
    {
        var ticket = new Ticket { AssignedAgentId = null };

        Assert.False(ticket.CanBeAssignedTo("agent-2", UserRole.Agent, "agent-1"));
    }

    [Fact]
    public void Agent_cannot_reassign_a_ticket_already_assigned_to_another_agent()
    {
        var ticket = new Ticket { AssignedAgentId = "agent-2" };

        Assert.False(ticket.CanBeAssignedTo("agent-1", UserRole.Agent, "agent-1"));
    }
}

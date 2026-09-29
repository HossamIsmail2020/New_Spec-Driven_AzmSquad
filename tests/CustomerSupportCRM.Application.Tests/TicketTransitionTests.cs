using CustomerSupportCRM.Domain.Entities;
using CustomerSupportCRM.Domain.Enums;
using Xunit;

namespace CustomerSupportCRM.Application.Tests;

/// <summary>T004 (spec FR-010, plan.md §4 transition table). Written before Ticket exists -- expected to fail to compile until T006 lands.</summary>
public class TicketTransitionTests
{
    [Theory]
    [InlineData(TicketStatus.Open, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Open, TicketStatus.Waiting, true)]
    [InlineData(TicketStatus.Open, TicketStatus.Closed, true)]
    [InlineData(TicketStatus.Open, TicketStatus.Resolved, false)]
    [InlineData(TicketStatus.Open, TicketStatus.Open, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Waiting, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Closed, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Open, false)]
    [InlineData(TicketStatus.Waiting, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Waiting, TicketStatus.Resolved, true)]
    [InlineData(TicketStatus.Waiting, TicketStatus.Closed, true)]
    [InlineData(TicketStatus.Waiting, TicketStatus.Open, false)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Open, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.InProgress, false)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Waiting, false)]
    [InlineData(TicketStatus.Closed, TicketStatus.Open, true)]
    [InlineData(TicketStatus.Closed, TicketStatus.InProgress, false)]
    [InlineData(TicketStatus.Closed, TicketStatus.Resolved, false)]
    [InlineData(TicketStatus.Closed, TicketStatus.Waiting, false)]
    public void IsValidTransition_matches_the_plan_transition_table(TicketStatus from, TicketStatus to, bool expected)
    {
        Assert.Equal(expected, Ticket.IsValidTransition(from, to));
    }

    [Fact]
    public void TransitionTo_throws_and_leaves_status_unchanged_on_an_invalid_transition()
    {
        var ticket = new Ticket { Status = TicketStatus.Open };

        Assert.Throws<InvalidOperationException>(() => ticket.TransitionTo(TicketStatus.Resolved));
        Assert.Equal(TicketStatus.Open, ticket.Status);
    }

    [Fact]
    public void TransitionTo_updates_status_on_a_valid_transition()
    {
        var ticket = new Ticket { Status = TicketStatus.Open };

        ticket.TransitionTo(TicketStatus.InProgress);

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using CustomerSupportCRM.Application.Auth;
using CustomerSupportCRM.Application.Customers;
using CustomerSupportCRM.Application.Dashboard;
using CustomerSupportCRM.Application.Tickets;
using CustomerSupportCRM.Domain.Enums;
using Xunit;
using static CustomerSupportCRM.Api.Tests.CustomWebApplicationFactory;

namespace CustomerSupportCRM.Api.Tests;

/// <summary>T018 (spec FR-016..FR-018). Written before IDashboardService/DashboardController exists -- expected to fail until T019 lands.</summary>
public class DashboardApiTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _adminClient = null!;
    private HttpClient _agentOneClient = null!;
    private string _agentOneId = null!;
    private string _agentTwoId = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        await _factory.CreateUserAsync("admin@test.dev", "Passw0rd!", "Test Admin", UserRole.Administrator);
        _agentOneId = await _factory.CreateUserAsync("agent1@test.dev", "Passw0rd!", "Agent One", UserRole.Agent);
        _agentTwoId = await _factory.CreateUserAsync("agent2@test.dev", "Passw0rd!", "Agent Two", UserRole.Agent);

        _adminClient = await AuthenticatedClientAsync("admin@test.dev");
        _agentOneClient = await AuthenticatedClientAsync("agent1@test.dev");
    }

    public Task DisposeAsync()
    {
        _adminClient.Dispose();
        _agentOneClient.Dispose();
        return _factory.DisposeAsync().AsTask();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Passw0rd!"), JsonOptions);
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private async Task<TicketDto> CreateTicketAssignedToAsync(string agentId)
    {
        var customerResponse = await _adminClient.PostAsJsonAsync("/api/customers",
            new CreateCustomerRequest("Test Customer", $"{Guid.NewGuid()}@example.com", null, null, null), JsonOptions);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>(JsonOptions);

        var ticketResponse = await _adminClient.PostAsJsonAsync("/api/tickets",
            new CreateTicketRequest("Subject", "Description", customer!.Id, TicketCategory.General, TicketPriority.Low), JsonOptions);
        var ticket = (await ticketResponse.Content.ReadFromJsonAsync<TicketDto>(JsonOptions))!;

        await _adminClient.PostAsJsonAsync($"/api/tickets/{ticket.Id}/assign", new AssignTicketRequest(agentId), JsonOptions);
        return ticket;
    }

    [Fact]
    public async Task Dashboard_shows_only_the_calling_agents_own_tickets()
    {
        var ticketForAgentOne = await CreateTicketAssignedToAsync(_agentOneId);
        await CreateTicketAssignedToAsync(_agentTwoId);

        var dashboard = await _agentOneClient.GetFromJsonAsync<AgentDashboardDto>("/api/dashboard/me", JsonOptions);

        Assert.Equal(1, dashboard!.TotalAssigned);
        Assert.Equal(ticketForAgentOne.Id, dashboard.AssignedTickets.Single().Id);
    }

    [Fact]
    public async Task Dashboard_reports_correct_per_status_counts()
    {
        var ticket = await CreateTicketAssignedToAsync(_agentOneId);
        await _adminClient.PostAsJsonAsync($"/api/tickets/{ticket.Id}/status", new ChangeTicketStatusRequest(TicketStatus.InProgress), JsonOptions);

        var dashboard = await _agentOneClient.GetFromJsonAsync<AgentDashboardDto>("/api/dashboard/me", JsonOptions);

        Assert.Contains(dashboard!.CountsByStatus, c => c.Status == "InProgress" && c.Count == 1);
    }
}

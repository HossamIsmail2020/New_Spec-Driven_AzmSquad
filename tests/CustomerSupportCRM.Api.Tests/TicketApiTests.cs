using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CustomerSupportCRM.Application.Auth;
using CustomerSupportCRM.Application.Customers;
using CustomerSupportCRM.Application.Tickets;
using CustomerSupportCRM.Domain.Enums;
using Xunit;
using static CustomerSupportCRM.Api.Tests.CustomWebApplicationFactory;

namespace CustomerSupportCRM.Api.Tests;

/// <summary>T016 (spec FR-006..FR-012). Written before TicketsController exists -- expected to fail until T017 lands.</summary>
public class TicketApiTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _adminClient = null!;
    private HttpClient _agentOneClient = null!;
    private HttpClient _agentTwoClient = null!;
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
        _agentTwoClient = await AuthenticatedClientAsync("agent2@test.dev");
    }

    public Task DisposeAsync()
    {
        _adminClient.Dispose();
        _agentOneClient.Dispose();
        _agentTwoClient.Dispose();
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

    private async Task<TicketDto> CreateTicketAsync()
    {
        var customerResponse = await _adminClient.PostAsJsonAsync("/api/customers",
            new CreateCustomerRequest("Test Customer", $"{Guid.NewGuid()}@example.com", null, null, null), JsonOptions);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>(JsonOptions);

        var response = await _adminClient.PostAsJsonAsync("/api/tickets",
            new CreateTicketRequest("Login issue", "Cannot log in.", customer!.Id, TicketCategory.Account, TicketPriority.High), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TicketDto>(JsonOptions))!;
    }

    [Fact]
    public async Task New_ticket_starts_Open_with_a_history_entry()
    {
        var ticket = await CreateTicketAsync();
        Assert.Equal(TicketStatus.Open, ticket.Status);

        var history = await _adminClient.GetFromJsonAsync<List<TicketHistoryEntryDto>>($"/api/tickets/{ticket.Id}/history", JsonOptions);
        Assert.Single(history!);
        Assert.Equal("Created", history![0].Type);
    }

    [Fact]
    public async Task Invalid_status_transition_is_rejected_and_status_is_unchanged()
    {
        var ticket = await CreateTicketAsync();

        var response = await _adminClient.PostAsJsonAsync($"/api/tickets/{ticket.Id}/status",
            new ChangeTicketStatusRequest(TicketStatus.Resolved), JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var reloaded = await _adminClient.GetFromJsonAsync<TicketDto>($"/api/tickets/{ticket.Id}", JsonOptions);
        Assert.Equal(TicketStatus.Open, reloaded!.Status);
    }

    [Fact]
    public async Task Valid_status_transition_is_accepted_and_logged()
    {
        var ticket = await CreateTicketAsync();

        var response = await _adminClient.PostAsJsonAsync($"/api/tickets/{ticket.Id}/status",
            new ChangeTicketStatusRequest(TicketStatus.InProgress), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var history = await _adminClient.GetFromJsonAsync<List<TicketHistoryEntryDto>>($"/api/tickets/{ticket.Id}/history", JsonOptions);
        Assert.Equal(2, history!.Count);
    }

    [Fact]
    public async Task Administrator_can_assign_a_ticket_to_any_agent()
    {
        var ticket = await CreateTicketAsync();

        var response = await _adminClient.PostAsJsonAsync($"/api/tickets/{ticket.Id}/assign",
            new AssignTicketRequest(_agentOneId), JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Agent_can_self_assign_an_unassigned_ticket()
    {
        var ticket = await CreateTicketAsync();

        var response = await _agentOneClient.PostAsJsonAsync($"/api/tickets/{ticket.Id}/assign",
            new AssignTicketRequest(_agentOneId), JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Agent_cannot_assign_to_a_different_agent()
    {
        var ticket = await CreateTicketAsync();

        var response = await _agentOneClient.PostAsJsonAsync($"/api/tickets/{ticket.Id}/assign",
            new AssignTicketRequest(_agentTwoId), JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

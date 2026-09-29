using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CustomerSupportCRM.Application.Auth;
using CustomerSupportCRM.Domain.Enums;
using Xunit;
using static CustomerSupportCRM.Api.Tests.CustomWebApplicationFactory;

namespace CustomerSupportCRM.Api.Tests;

/// <summary>T012 (spec FR-013, FR-014, FR-015). Written before AuthController exists -- expected to fail until T013 lands.</summary>
public class AuthApiTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeDatabaseAsync();
    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    [Fact]
    public async Task Login_with_valid_credentials_returns_a_token()
    {
        await _factory.CreateUserAsync("admin@test.dev", "Passw0rd!", "Test Admin", UserRole.Administrator);
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@test.dev", "Passw0rd!"), JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
    }

    [Fact]
    public async Task Login_with_invalid_credentials_is_rejected()
    {
        await _factory.CreateUserAsync("admin2@test.dev", "Passw0rd!", "Test Admin", UserRole.Administrator);
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin2@test.dev", "WrongPassword!"), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_request_to_a_protected_endpoint_is_rejected()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Non_administrator_cannot_register_an_agent()
    {
        await _factory.CreateUserAsync("agent@test.dev", "Passw0rd!", "Test Agent", UserRole.Agent);
        using var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("agent@test.dev", "Passw0rd!"), JsonOptions);
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);

        var response = await client.PostAsJsonAsync("/api/auth/register-agent",
            new RegisterAgentRequest("New Agent", "new-agent@test.dev", "Passw0rd!"), JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_response_never_contains_a_password_field()
    {
        await _factory.CreateUserAsync("admin3@test.dev", "Passw0rd!", "Test Admin", UserRole.Administrator);
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin3@test.dev", "Passw0rd!"), JsonOptions);
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("password", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Passw0rd!", raw);
    }
}

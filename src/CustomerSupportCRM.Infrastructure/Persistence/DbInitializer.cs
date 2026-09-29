using CustomerSupportCRM.Domain.Entities;
using CustomerSupportCRM.Domain.Enums;
using CustomerSupportCRM.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCRM.Infrastructure.Persistence;

/// <summary>Dev-only seed data so the app is demoable immediately after `dotnet ef database update` (plan.md §6 dev experience goal). Not run in production.</summary>
public static class DbInitializer
{
    public const string SeedAdminEmail = "admin@azmsquad-sdd.dev";
    public const string SeedAgentEmail = "agent@azmsquad-sdd.dev";
    public const string SeedPassword = "Passw0rd!";

    public static async Task SeedAsync(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        await db.Database.MigrateAsync();

        var admin = await EnsureUserAsync(userManager, SeedAdminEmail, "Admin User", UserRole.Administrator);
        var agent = await EnsureUserAsync(userManager, SeedAgentEmail, "Agent User", UserRole.Agent);

        if (await db.Customers.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Sample Customer",
            Email = "customer@example.com",
            Phone = "+201000000000",
            Company = "Example LLC",
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Customers.Add(customer);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Subject = "Cannot log in to my account",
            Description = "I reset my password but still cannot log in.",
            CustomerId = customer.Id,
            Category = TicketCategory.Account,
            Priority = TicketPriority.High,
            Status = TicketStatus.Open,
            AssignedAgentId = agent.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Tickets.Add(ticket);

        db.TicketHistoryEntries.Add(new TicketHistoryEntry
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Type = TicketHistoryEntryType.Created,
            Description = "Ticket created with priority High.",
            ActorUserId = admin.Id,
            CreatedAt = now
        });

        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager, string email, string fullName, UserRole role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            Role = role,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, SeedPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed user '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }
}

using CustomerSupportCRM.Infrastructure.Identity;
using CustomerSupportCRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerSupportCRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=crm.db";
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        // Note: no .AddSignInManager() -- that method doesn't exist on this
        // IdentityBuilder shape (confirmed by the prior session's compiler
        // error) and isn't needed for a JWT-only API; AuthService uses
        // UserManager.CheckPasswordAsync directly.
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        return services;
    }
}

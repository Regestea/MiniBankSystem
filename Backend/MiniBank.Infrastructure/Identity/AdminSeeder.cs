using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MiniBank.Infrastructure.Identity;

/// <summary>
/// Development seeding: ensures the single-level Admin role and one admin user
/// (from configuration) exist after migrations. No new tables — AspNetRoles/AspNetUserRoles.
/// </summary>
public static class AdminSeeder
{
    private const string AdminRole = "Admin";
    private const string UserRole = "User";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("AdminSeeder");

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { AdminRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger?.LogInformation("Created role '{Role}'.", role);
            }
        }

        var section = configuration.GetSection("Seed:Admin");
        var email = section["Email"];
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return; // nothing to seed

        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        if (await userManager.FindByEmailAsync(email) is not null)
            return; // already seeded

        var admin = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, AdminRole);
            await userManager.AddToRoleAsync(admin, UserRole);
            logger?.LogInformation("Seeded admin user '{Email}' with roles Admin+User.", email);
        }
        else
        {
            logger?.LogError("Failed to seed admin '{Email}': {Errors}",
                email, string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}

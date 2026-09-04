using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MiniBank.Infrastructure.Identity;

/// <summary>Seeds Admin role and admin user from configuration using Guid Identity.</summary>
public sealed class AdminSeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<IdentityUser<Guid>> userManager,
    ILogger<AdminSeeder> logger,
    IConfiguration configuration)
{
    private const string AdminRole = "Admin";
    private const string UserRole = "User";

    public async Task SeedAsync()
    {
        foreach (var role in new[] { AdminRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                logger.LogInformation("Created role '{Role}'.", role);
            }
        }

        var section = configuration.GetSection("Seed:Admin");
        var email = section["Email"];
        var password = section["Password"];

        // Fall back to environment variables if config placeholders are empty
        if (string.IsNullOrWhiteSpace(email) || email.Contains("${"))
            email = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        if (string.IsNullOrWhiteSpace(password) || password.Contains("${"))
            password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return; // nothing to seed

        if (await userManager.FindByEmailAsync(email) is not null)
            return; // already seeded

        var admin = new IdentityUser<Guid> { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, AdminRole);
            await userManager.AddToRoleAsync(admin, UserRole);
            logger.LogInformation("Seeded admin user '{Email}' with roles Admin+User.", email);
        }
        else
        {
            logger.LogError("Failed to seed admin '{Email}': {Errors}",
                email, string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}

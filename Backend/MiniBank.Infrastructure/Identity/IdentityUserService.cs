using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MiniBank.Abstractions;

namespace MiniBank.Infrastructure.Identity;

/// <summary>
/// Infrastructure implementation of <see cref="IIdentityUserService"/> using UserManager.
/// Keeps Features layer free from ASP.NET Core Identity dependencies.
/// </summary>
internal sealed class IdentityUserService(
    UserManager<IdentityUser<Guid>> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    ILogger<IdentityUserService> logger) : IIdentityUserService
{
    private const string UserRole = "User";

    public async Task CreateUserAsync(Guid userId, string email, string password, CancellationToken cancellationToken = default)
    {
        var user = new IdentityUser<Guid>(userId.ToString())
        {
            Id = userId,
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create IdentityUser: {errors}");
        }
    }

    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null;
    }

    public async Task<string?> GetEmailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user?.Email;
    }

    public async Task EnsureUserRoleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            logger.LogWarning("EnsureUserRole: IdentityUser {UserId} not found.", userId);
            return;
        }

        if (!await roleManager.RoleExistsAsync(UserRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(UserRole));
            if (!roleResult.Succeeded)
            {
                logger.LogError("Failed to create role '{Role}': {Errors}", UserRole, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (await userManager.IsInRoleAsync(user, UserRole))
            return;

        var result = await userManager.AddToRoleAsync(user, UserRole);
        if (result.Succeeded)
            logger.LogInformation("Assigned role '{Role}' to user {UserId}.", UserRole, userId);
        else
            logger.LogWarning("Failed to assign role '{Role}' to user {UserId}: {Errors}", UserRole, userId, string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}

namespace MiniBank.Abstractions;

/// <summary>
/// Abstraction over ASP.NET Core Identity for Features layer.
/// Keeps Domain/Features independent from Microsoft.AspNetCore.Identity.
/// Implemented in Infrastructure via UserManager{IdentityUser{Guid}}.
/// </summary>
public interface IIdentityUserService
{
    /// <summary>
    /// Creates a new IdentityUser via UserManager (persists immediately — NOT staged).
    /// Callers must compensate (see <see cref="DeleteUserAsync"/>) if later domain persistence fails.
    /// </summary>
    Task CreateUserAsync(Guid userId, string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compensation for a failed registration: deletes an orphan IdentityUser
    /// created by <see cref="CreateUserAsync"/> when Customer/Risk persistence fails.
    /// Best-effort — logs and swallows store errors so the original failure surfaces.
    /// </summary>
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Returns true if an IdentityUser with <paramref name="userId"/> exists.</summary>
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Returns the normalized email for the IdentityUser, or null if not found.</summary>
    Task<string?> GetEmailAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Ensures the IdentityUser has the "User" role, persisting it if missing.</summary>
    Task EnsureUserRoleAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns IdentityUser IDs that exist in Identity but have no corresponding Customer record.
    /// Used by reconciliation job to clean up orphans from failed two-phase registrations.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetOrphanUserIdsAsync(CancellationToken cancellationToken = default);
}

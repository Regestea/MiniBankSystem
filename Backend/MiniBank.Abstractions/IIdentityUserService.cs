namespace MiniBank.Abstractions;

/// <summary>
/// Abstraction over ASP.NET Core Identity for Features layer.
/// Keeps Domain/Features independent from Microsoft.AspNetCore.Identity.
/// Implemented in Infrastructure via UserManager{IdentityUser{Guid}}.
/// </summary>
public interface IIdentityUserService
{
    /// <summary>
    /// Stages a new IdentityUser in the change tracker (does NOT call SaveChanges).
    /// The caller must call IUnitOfWork.SaveChangesAsync to persist both Identity and Domain atomically.
    /// </summary>
    Task CreateUserAsync(Guid userId, string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Returns true if an IdentityUser with <paramref name="userId"/> exists.</summary>
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Returns the normalized email for the IdentityUser, or null if not found.</summary>
    Task<string?> GetEmailAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Ensures the IdentityUser has the "User" role, persisting it if missing.</summary>
    Task EnsureUserRoleAsync(Guid userId, CancellationToken cancellationToken = default);
}

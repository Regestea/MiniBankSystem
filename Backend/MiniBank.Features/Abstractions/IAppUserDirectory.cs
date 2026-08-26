namespace MiniBank.Features.Abstractions;

/// <summary>Read/attach access to identity users — implemented over UserManager in Infrastructure.</summary>
public interface IAppUserDirectory
{
    Task<UserSnapshot?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Stamps the user's CustomerId (no save — commit via IUnitOfWork).</summary>
    /// <returns>false when the user does not exist.</returns>
    Task<bool> TryAttachCustomerAsync(Guid userId, Guid customerId, CancellationToken cancellationToken = default);

    Task EnsureUserRoleAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record UserSnapshot(Guid UserId, string? Email, Guid? CustomerId);

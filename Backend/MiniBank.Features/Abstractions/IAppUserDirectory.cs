namespace MiniBank.Features.Abstractions;

public interface IAppUserDirectory
{
    Task<UserSnapshot?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> TryAttachCustomerAsync(Guid userId, Guid customerId, CancellationToken cancellationToken = default);

    Task EnsureUserRoleAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record UserSnapshot(Guid UserId, string? Email, Guid? CustomerId);

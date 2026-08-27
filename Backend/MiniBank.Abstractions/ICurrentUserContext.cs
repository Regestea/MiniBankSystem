namespace MiniBank.Abstractions;

/// <summary>Provides current caller identity for ownership checks.</summary>
public interface ICurrentUserContext
{
    Guid UserId { get; }
    Task<Guid?> GetCustomerIdAsync(CancellationToken cancellationToken = default);
}

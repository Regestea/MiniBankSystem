namespace MiniBank.Features.Abstractions;

public interface ICurrentUserContext
{
    Guid UserId { get; }
    Task<Guid?> GetCustomerIdAsync(CancellationToken cancellationToken = default);
}

using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Features;

/// <summary>
/// Ensures the owning customer is not Blocked before a write operation on their accounts.
/// Blocking a customer must immediately stop all money movement, regardless of token validity.
/// </summary>
internal interface ICustomerAccessGuard
{
    /// <summary>Throws ForbiddenException when the customer is Blocked. Admins bypass (they act on behalf of the bank).</summary>
    Task EnsureNotBlockedAsync(CustomerId customerId, CancellationToken cancellationToken = default);
}

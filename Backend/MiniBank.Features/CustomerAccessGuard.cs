using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Features;

/// <summary>Load-based check against ICustomerRepository — always reflects the current DB status.</summary>
internal sealed class CustomerAccessGuard(ICustomerRepository customers, ICurrentUserContext currentUser) : ICustomerAccessGuard
{
    public async Task EnsureNotBlockedAsync(CustomerId customerId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IsAdmin)
            return;

        var customer = await customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException("customer", customerId);

        if (customer.Status == CustomerStatus.Blocked)
            throw new ForbiddenException("customer", "Customer is blocked. Transactions are not allowed.");
    }
}

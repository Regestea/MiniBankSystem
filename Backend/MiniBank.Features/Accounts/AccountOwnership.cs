using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Abstractions;

namespace MiniBank.Features.Accounts;

internal static class AccountOwnership
{
    public static async Task EnsureOwnedByCallerAsync(
        Domain.CustomerAggregate.ValueObjects.CustomerId accountOwnerId,
        ICurrentUserContext currentUser)
    {
        var callerCustomerId = await currentUser.GetCustomerIdAsync()
            ?? throw new ForbiddenException("customer", "User has no linked customer profile.");

        if (!accountOwnerId.Equals(callerCustomerId))
            throw new ForbiddenException("account", "Account is not owned by the current user.");
    }
}

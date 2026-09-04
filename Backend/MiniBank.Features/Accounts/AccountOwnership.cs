using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Abstractions;

namespace MiniBank.Features.Accounts;

internal static class AccountOwnership
{
    /// <summary>Ownership is a plain Guid comparison: customer id == identity user id. Admins bypass (consistent with AccessGuard).</summary>
    public static void EnsureOwnedByCaller(
        Domain.CustomerAggregate.ValueObjects.CustomerId accountOwnerId,
        ICurrentUserContext currentUser)
    {
        if (currentUser.IsAdmin)
            return;

        if (!accountOwnerId.Value.Equals(currentUser.UserId))
            throw new ForbiddenException("account", "Account is not owned by the current user.");
    }
}

using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.CloseAccount;

internal sealed class CloseAccountHandler(
    IAccountRepository accounts,
    ICustomerAccessGuard customerAccess,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<CloseAccountCommand, CloseAccountResponse>
{
    public async Task<CloseAccountResponse> HandleAsync(CloseAccountCommand command, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            var account = await accounts.LoadAsync(command.AccountId, cancellationToken)
                ?? throw new NotFoundException("account", command.AccountId);

            AccountOwnership.EnsureOwnedByCaller(account.CustomerId, currentUser);
            await customerAccess.EnsureNotBlockedAsync(account.CustomerId, cancellationToken);

            account.Close();

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return new CloseAccountResponse(account.Id.Value, account.Status.ToString(), account.Version);
            }
            catch (ConcurrencyConflictException) when (attempt < maxRetries - 1)
            {
                // TOCTOU (e.g. concurrent Deposit vs Close): reload and re-evaluate
                // Balance.IsZero on fresh state so the caller gets a clean domain
                // error instead of a raw 409.
                unitOfWork.DetachAll();
            }
        }

        // Final attempt without swallowing: surface the conflict if still contended.
        var finalAccount = await accounts.LoadAsync(command.AccountId, cancellationToken)
            ?? throw new NotFoundException("account", command.AccountId);
        AccountOwnership.EnsureOwnedByCaller(finalAccount.CustomerId, currentUser);
        await customerAccess.EnsureNotBlockedAsync(finalAccount.CustomerId, cancellationToken);
        finalAccount.Close();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CloseAccountResponse(finalAccount.Id.Value, finalAccount.Status.ToString(), finalAccount.Version);
    }
}

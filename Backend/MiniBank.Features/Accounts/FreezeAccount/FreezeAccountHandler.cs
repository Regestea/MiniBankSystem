using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.FreezeAccount;

internal sealed class FreezeAccountHandler(
    IAccountRepository accounts,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<FreezeAccountCommand, AccountStatusResponse>
{
    public async Task<AccountStatusResponse> HandleAsync(FreezeAccountCommand command, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAdmin)
            throw new ForbiddenException("account", "Only admins can freeze accounts.");

        const int maxRetries = 3;
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            var account = await accounts.LoadAsync(command.AccountId, cancellationToken)
                ?? throw new NotFoundException("account", command.AccountId);

            account.Freeze();

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return new AccountStatusResponse(account.Id.Value, account.Status.ToString(), account.Version);
            }
            catch (ConcurrencyConflictException) when (attempt < maxRetries - 1)
            {
                unitOfWork.DetachAll();
            }
        }

        var finalAccount = await accounts.LoadAsync(command.AccountId, cancellationToken)
            ?? throw new NotFoundException("account", command.AccountId);
        finalAccount.Freeze();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new AccountStatusResponse(finalAccount.Id.Value, finalAccount.Status.ToString(), finalAccount.Version);
    }
}

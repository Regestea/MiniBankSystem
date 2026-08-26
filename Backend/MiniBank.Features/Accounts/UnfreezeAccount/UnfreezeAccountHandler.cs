using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.UnfreezeAccount;

internal sealed class UnfreezeAccountHandler(
    IAccountRepository accounts,
    IUnitOfWork unitOfWork) : ICommandHandler<UnfreezeAccountCommand, AccountStatusResponse>
{
    public async Task<AccountStatusResponse> Handle(UnfreezeAccountCommand command, CancellationToken cancellationToken = default)
    {
        var account = await accounts.LoadAsync(command.AccountId, cancellationToken)
            ?? throw new NotFoundException("account", command.AccountId);

        account.Unfreeze(); // raises AccountUnfrozenEvent — transactions allowed again

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AccountStatusResponse(account.Id.Value, account.Status.ToString(), account.Version);
    }
}

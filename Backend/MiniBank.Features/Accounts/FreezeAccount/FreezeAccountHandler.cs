using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.FreezeAccount;

internal sealed class FreezeAccountHandler(
    IAccountRepository accounts,
    IUnitOfWork unitOfWork) : ICommandHandler<FreezeAccountCommand, AccountStatusResponse>
{
    public async Task<AccountStatusResponse> HandleAsync(FreezeAccountCommand command, CancellationToken cancellationToken = default)
    {
        var account = await accounts.LoadAsync(command.AccountId, cancellationToken)
            ?? throw new NotFoundException("account", command.AccountId);

        account.Freeze();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AccountStatusResponse(account.Id.Value, account.Status.ToString(), account.Version);
    }
}

using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.CloseAccount;

internal sealed class CloseAccountHandler(
    IAccountRepository accounts,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<CloseAccountCommand, CloseAccountResponse>
{
    public async Task<CloseAccountResponse> Handle(CloseAccountCommand command, CancellationToken cancellationToken = default)
    {
        var account = await accounts.LoadAsync(command.AccountId, cancellationToken)
            ?? throw new NotFoundException("account", command.AccountId);

        await AccountOwnership.EnsureOwnedByCallerAsync(account.CustomerId, currentUser);

        account.Close(); // domain guards: not Frozen, zero balance

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CloseAccountResponse(account.Id.Value, account.Status.ToString(), account.Version);
    }
}

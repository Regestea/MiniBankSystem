using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.CloseAccount;

internal sealed class CloseAccountHandler(
    IAccountRepository accounts,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<CloseAccountCommand, CloseAccountResponse>
{
    public async Task<CloseAccountResponse> HandleAsync(CloseAccountCommand command, CancellationToken cancellationToken = default)
    {
        var account = await accounts.LoadAsync(command.AccountId, cancellationToken)
            ?? throw new NotFoundException("account", command.AccountId);

        AccountOwnership.EnsureOwnedByCaller(account.CustomerId, currentUser);

        account.Close();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CloseAccountResponse(account.Id.Value, account.Status.ToString(), account.Version);
    }
}

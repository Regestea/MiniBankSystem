using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.RejectAccount;

internal sealed class RejectAccountHandler(
    IAccountRepository accounts,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<RejectAccountCommand, RejectAccountResponse>
{
    public async Task<RejectAccountResponse> HandleAsync(RejectAccountCommand command, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAdmin)
            throw new ForbiddenException("account", "Only admins can reject accounts.");

        var account = await accounts.LoadAsync(new AccountId(command.AccountId), cancellationToken)
            ?? throw new NotFoundException("account", command.AccountId);

        account.Reject(command.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RejectAccountResponse(account.Id.Value, account.Status.ToString(), account.Version);
    }
}

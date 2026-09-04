using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.ApproveAccount;

internal sealed class ApproveAccountHandler(
    IAccountRepository accounts,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<ApproveAccountCommand, ApproveAccountResponse>
{
    public async Task<ApproveAccountResponse> HandleAsync(ApproveAccountCommand command, CancellationToken cancellationToken = default)
    {
        // Defense in depth: controller already requires Admin role; re-check here so
        // direct mediator callers cannot bypass authorization.
        if (!currentUser.IsAdmin)
            throw new ForbiddenException("account", "Only admins can approve accounts.");

        var account = await accounts.LoadAsync(new AccountId(command.AccountId), cancellationToken)
            ?? throw new NotFoundException("account", command.AccountId);

        account.Approve();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ApproveAccountResponse(account.Id.Value, account.Status.ToString(), account.Version);
    }
}

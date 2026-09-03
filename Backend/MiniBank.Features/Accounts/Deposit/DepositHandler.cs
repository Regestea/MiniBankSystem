using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.Deposit;

internal sealed class DepositHandler(
    IAccountRepository accounts,
    ICustomerAccessGuard customerAccess,
    ITransactionRepository transactions,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<DepositCommand, TransactionResponse>
{
    public async Task<TransactionResponse> HandleAsync(DepositCommand command, CancellationToken cancellationToken = default)
    {
        var account = await accounts.LoadAsync(command.AccountId, cancellationToken)
            ?? throw new NotFoundException("account", command.AccountId);

        AccountOwnership.EnsureOwnedByCaller(account.CustomerId, currentUser);
        await customerAccess.EnsureNotBlockedAsync(account.CustomerId, cancellationToken);

        var (tx, _) = account.Deposit(Money.FromDecimal(command.Amount));

        await transactions.AddAsync(tx, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new TransactionResponse(tx.Id.Value, tx.Type.ToString(), tx.Amount.Amount,
                                       tx.ReferenceId, tx.OccurredOn);
    }
}

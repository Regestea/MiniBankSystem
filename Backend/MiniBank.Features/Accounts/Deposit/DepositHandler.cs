using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Features.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.Deposit;

internal sealed class DepositHandler(
    IAccountRepository accounts,
    ITransactionRepository transactions,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<DepositCommand, TransactionResponse>
{
    public async Task<TransactionResponse> Handle(DepositCommand command, CancellationToken cancellationToken = default)
    {
        var account = await accounts.LoadAsync(command.AccountId, cancellationToken)
            ?? throw new NotFoundException("account", command.AccountId);

        await AccountOwnership.EnsureOwnedByCallerAsync(account.CustomerId, currentUser);

        var (tx, _) = account.Deposit(Money.FromDecimal(command.Amount));

        await transactions.AddAsync(tx, cancellationToken);   // journal
        await unitOfWork.SaveChangesAsync(cancellationToken); // account.ledger_entries + transactions atomically

        return new TransactionResponse(tx.Id.Value, tx.Type.ToString(), tx.Amount.Amount,
                                       tx.ReferenceId, tx.OccurredOn);
    }
}

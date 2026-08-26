using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Features.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.Transfer;

/// <summary>
/// Atomic double-entry transfer: one Transaction (TransferOut + TransferIn postings)
/// plus both account ledgers committed in a single SaveChanges.
/// Ownership enforced on the SOURCE account; anyone may receive money.
/// </summary>
internal sealed class TransferHandler(
    IAccountRepository accounts,
    ITransactionRepository transactions,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<TransferCommand, TransferResponse>
{
    public async Task<TransferResponse> Handle(TransferCommand command, CancellationToken cancellationToken = default)
    {
        var from = await accounts.LoadAsync(command.FromAccountId, cancellationToken)
            ?? throw new NotFoundException("account", command.FromAccountId);

        await AccountOwnership.EnsureOwnedByCallerAsync(from.CustomerId, currentUser);

        var to = await accounts.LoadAsync(command.ToAccountId, cancellationToken)
            ?? throw new NotFoundException("account", command.ToAccountId);

        // Sufficient funds (ordered balance) + Active statuses validated inside the Transaction aggregate
        var tx = from.TransferTo(to, Money.FromDecimal(command.Amount));

        await transactions.AddAsync(tx, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);   // both ledgers + journal atomically

        return new TransferResponse(tx.Id.Value, tx.Amount.Amount, tx.ReferenceId,
                                    tx.ToTransferEntries().FromEntry.Id,
                                    tx.ToTransferEntries().ToEntry.Id,
                                    tx.OccurredOn);
    }
}

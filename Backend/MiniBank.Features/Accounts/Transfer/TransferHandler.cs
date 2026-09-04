using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.RiskAggregate;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.Transfer;

/// <summary>Double-entry transfer between accounts.</summary>
internal sealed class TransferHandler(
    IAccountRepository accounts,
    ICustomerAccessGuard customerAccess,
    ITransactionRepository transactions,
    IRiskRepository riskRepo,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<TransferCommand, TransferResponse>
{
    private const int MaxRetries = 3;

    public async Task<TransferResponse> HandleAsync(TransferCommand command, CancellationToken cancellationToken = default)
    {
        var idempotencyKey = IdempotencyKeys.Normalize(command.IdempotencyKey);

        var fromId = new MiniBank.Domain.AccountAggregate.ValueObjects.AccountId(command.FromAccountId);
        var toId = new MiniBank.Domain.AccountAggregate.ValueObjects.AccountId(command.ToAccountId);

        // Only the sender must be active; incoming money to a blocked customer's account is allowed (banking convention).
        // NOTE: destination account must still be Active (see Account.TransferTo) — frozen/pending
        // destinations are rejected. Blocked *customers* may still receive money.

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            // Lock ordering: always load both accounts in ascending Guid order so that
            // concurrent A->B and B->A transfers take row locks in the same order.
            // This reduces 40P01 deadlocks; remaining conflicts are retried (see catch below).
            // NOTE: EF SELECTs alone don't take row locks — the ordering also keeps the
            // change-tracker/UPDATE order deterministic. EfUnitOfWork maps 40P01 to
            // ConcurrencyConflictException so it stays retryable.
            Account from;
            Account to;
            if (command.FromAccountId.CompareTo(command.ToAccountId) < 0)
            {
                from = await accounts.LoadAsync(fromId, cancellationToken)
                    ?? throw new NotFoundException("account", command.FromAccountId);
                to = await accounts.LoadAsync(toId, cancellationToken)
                    ?? throw new NotFoundException("account", command.ToAccountId);
            }
            else
            {
                to = await accounts.LoadAsync(toId, cancellationToken)
                    ?? throw new NotFoundException("account", command.ToAccountId);
                from = await accounts.LoadAsync(fromId, cancellationToken)
                    ?? throw new NotFoundException("account", command.FromAccountId);
            }

            AccountOwnership.EnsureOwnedByCaller(from.CustomerId, currentUser);
            await customerAccess.EnsureNotBlockedAsync(from.CustomerId, cancellationToken);

            var existing = await transactions.GetByReferenceIdAsync(idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                EnsureIdempotentMatch(existing, command);
                return new TransferResponse(existing.Id.Value, existing.Amount.Amount, existing.ReferenceId,
                                            existing.SourceAccountId!.Value,
                                            existing.DestinationAccountId!.Value,
                                            existing.OccurredOn);
            }

            var risk = await riskRepo.GetByCustomerIdAsync(from.CustomerId.Value, cancellationToken);
            if (risk is null)
                throw new DomainInvariantViolationException("Risk",
                    "No risk assessment found for this customer. An admin must set a risk level before transactions are allowed.");

            if (!risk.CanTransact(command.Amount))
                throw new DomainInvariantViolationException("Amount",
                    $"Daily transaction limit exceeded. Limit: {risk.DailyTransactionLimit}, used today: {risk.AmountToday}.");

            var (tx, fromEntry, toEntry) = from.TransferTo(to, Money.FromDecimal(command.Amount), referenceId: idempotencyKey);

            to.ApplyInboundEntry(toEntry);

            await transactions.AddAsync(tx, cancellationToken);

            risk.RecordTransaction(command.Amount);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return new TransferResponse(tx.Id.Value, tx.Amount.Amount, tx.ReferenceId,
                                            from.Id.Value, to.Id.Value, tx.OccurredOn);
            }
            catch (ConcurrencyConflictException)
            {
                // Fresh ordered reload happens at the top of the next iteration.
                unitOfWork.DetachAll();
            }
            catch (UniqueConstraintViolationException)
            {
                // Concurrent duplicate with the same idempotency key: return the winner
                // (unless the winner has a different payload — then 409).
                unitOfWork.DetachAll();
                var winner = await transactions.GetByReferenceIdAsync(idempotencyKey, cancellationToken);
                if (winner is not null)
                {
                    EnsureIdempotentMatch(winner, command);
                    return new TransferResponse(winner.Id.Value, winner.Amount.Amount, winner.ReferenceId,
                                                winner.SourceAccountId!.Value,
                                                winner.DestinationAccountId!.Value,
                                                winner.OccurredOn);
                }
            }
        }

        throw IdempotencyKeys.RetryExhausted();
    }

    // NOTE: reference_id is GLOBAL (ux_transactions_reference). Reusing the same key with a
    // different amount/accounts must be a 409, not a silent return of someone else's transaction.
    private static void EnsureIdempotentMatch(Transaction existing, TransferCommand command)
    {
        if (existing.Type != TransactionType.Transfer
            || existing.Amount.Amount != command.Amount
            || existing.SourceAccountId?.Value != command.FromAccountId
            || existing.DestinationAccountId?.Value != command.ToAccountId)
            throw IdempotencyKeys.Mismatch("Idempotency key was already used with a different amount or accounts.");
    }
}

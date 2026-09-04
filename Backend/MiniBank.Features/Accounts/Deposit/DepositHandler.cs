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
    private const int MaxRetries = 3;

    // NOTE (sample-bank policy): deposits are intentionally OUTSIDE daily risk limits.
    // Limits (CanTransact/RecordTransaction) apply to outflows only (Withdraw/Transfer).
    // Inflows cannot drain an account, and counting them would let Withdraw->Deposit
    // cycling games inflate counters. Documented here so reviewers know it is deliberate.
    public async Task<TransactionResponse> HandleAsync(DepositCommand command, CancellationToken cancellationToken = default)
    {
        var idempotencyKey = IdempotencyKeys.Normalize(command.IdempotencyKey);

        var account = await accounts.LoadAsync(command.AccountId, cancellationToken)
            ?? throw new NotFoundException("account", command.AccountId);

        AccountOwnership.EnsureOwnedByCaller(account.CustomerId, currentUser);

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            await customerAccess.EnsureNotBlockedAsync(account.CustomerId, cancellationToken);

            var existing = await transactions.GetByReferenceIdAsync(idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                EnsureIdempotentMatch(existing, command);
                return new TransactionResponse(existing.Id.Value, existing.Type.ToString(), existing.Amount.Amount,
                                               existing.ReferenceId, existing.OccurredOn);
            }

            var (tx, _) = account.Deposit(Money.FromDecimal(command.Amount), referenceId: idempotencyKey);

            await transactions.AddAsync(tx, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return new TransactionResponse(tx.Id.Value, tx.Type.ToString(), tx.Amount.Amount,
                                               tx.ReferenceId, tx.OccurredOn);
            }
            catch (ConcurrencyConflictException)
            {
                unitOfWork.DetachAll();
                account = await accounts.LoadAsync(command.AccountId, cancellationToken)
                    ?? throw new NotFoundException("account", command.AccountId);
            }
            catch (UniqueConstraintViolationException)
            {
                // Concurrent duplicate with the same idempotency key passed the
                // check-then-insert race: re-read and return the winner idempotently.
                unitOfWork.DetachAll();
                var winner = await transactions.GetByReferenceIdAsync(idempotencyKey, cancellationToken);
                if (winner is not null)
                {
                    EnsureIdempotentMatch(winner, command);
                    return new TransactionResponse(winner.Id.Value, winner.Type.ToString(), winner.Amount.Amount,
                                                   winner.ReferenceId, winner.OccurredOn);
                }
                account = await accounts.LoadAsync(command.AccountId, cancellationToken)
                    ?? throw new NotFoundException("account", command.AccountId);
            }
        }

        throw IdempotencyKeys.RetryExhausted();
    }

    // NOTE: reference_id is GLOBAL (ux_transactions_reference). Reusing the same key with a
    // different amount/type/account must be a 409, not a silent return of someone else's transaction.
    private static void EnsureIdempotentMatch(Transaction existing, DepositCommand command)
    {
        if (existing.Type != TransactionType.Deposit
            || existing.Amount.Amount != command.Amount
            || existing.DestinationAccountId?.Value != command.AccountId)
            throw IdempotencyKeys.Mismatch();
    }
}

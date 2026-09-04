using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.RiskAggregate;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.Withdraw;

internal sealed class WithdrawHandler(
    IAccountRepository accounts,
    ICustomerAccessGuard customerAccess,
    ITransactionRepository transactions,
    IRiskRepository riskRepo,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<WithdrawCommand, TransactionResponse>
{
    private const int MaxRetries = 3;

    public async Task<TransactionResponse> HandleAsync(WithdrawCommand command, CancellationToken cancellationToken = default)
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

            var risk = await riskRepo.GetByCustomerIdAsync(account.CustomerId.Value, cancellationToken);
            if (risk is null)
                throw new DomainInvariantViolationException("Risk",
                    "No risk assessment found for this customer. An admin must set a risk level before transactions are allowed.");

            if (!risk.CanTransact(command.Amount))
                throw new DomainInvariantViolationException("Amount",
                    $"Daily transaction limit exceeded. Limit: {risk.DailyTransactionLimit}, used today: {risk.AmountToday}.");

            var (tx, _) = account.Withdraw(Money.FromDecimal(command.Amount), referenceId: idempotencyKey);

            await transactions.AddAsync(tx, cancellationToken);

            risk.RecordTransaction(command.Amount);

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
                // Same idempotency race as Deposit: return the winner instead of 409
                // (unless the winner has a different payload — then 409).
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
    private static void EnsureIdempotentMatch(Transaction existing, WithdrawCommand command)
    {
        if (existing.Type != TransactionType.Withdraw
            || existing.Amount.Amount != command.Amount
            || existing.SourceAccountId?.Value != command.AccountId)
            throw IdempotencyKeys.Mismatch();
    }
}

using Dapper;
using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BeneficiaryAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Domain.RiskAggregate;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.TransferByNumber;

/// <summary>Double-entry transfer where the destination is resolved by account number.</summary>
internal sealed class TransferByAccountNumberHandler(
    IAccountRepository accounts,
    ICustomerAccessGuard customerAccess,
    ITransactionRepository transactions,
    IBeneficiaryRepository beneficiaries,
    IRiskRepository riskRepo,
    ISqlConnectionFactory connectionFactory,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<TransferByAccountNumberCommand, TransferResponse>
{
    private const int MaxRetries = 3;

    public async Task<TransferResponse> HandleAsync(TransferByAccountNumberCommand command, CancellationToken cancellationToken = default)
    {
        var idempotencyKey = string.IsNullOrWhiteSpace(command.IdempotencyKey)
            ? $"transfer-{Guid.NewGuid():N}"
            : IdempotencyKeys.Normalize(command.IdempotencyKey);

        var normalizedTo = AccountNumber.Normalize(command.ToAccountNumber);
        // Validator already rejects bad formats (400); constructor would throw the same.
        _ = new AccountNumber(normalizedTo);

        var fromId = new AccountId(command.FromAccountId);

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var from = await accounts.LoadAsync(fromId, cancellationToken)
                ?? throw new NotFoundException("account", command.FromAccountId);
            var to = await accounts.LoadByNumberAsync(normalizedTo, cancellationToken)
                ?? throw new NotFoundException("account", command.ToAccountNumber);

            if (from.Id.Equals(to.Id))
                throw new DomainValidationException("Transfer", "Source and destination accounts must differ.");

            AccountOwnership.EnsureOwnedByCaller(from.CustomerId, currentUser);
            await customerAccess.EnsureNotBlockedAsync(from.CustomerId, cancellationToken);

            var existing = await transactions.GetByReferenceIdAsync(idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                EnsureIdempotentMatch(existing, command, from.Id.Value, to.Id.Value);
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

            // Load in ascending Guid order already handled by two separate loads above;
            // EF change-tracker UPDATE order stays deterministic for the common case.
            var (tx, fromEntry, toEntry) = from.TransferTo(to, Money.FromDecimal(command.Amount), referenceId: idempotencyKey);

            to.ApplyInboundEntry(toEntry);

            await transactions.AddAsync(tx, cancellationToken);

            risk.RecordTransaction(command.Amount);

            if (command.SaveBeneficiary)
                await SaveBeneficiaryIfNewAsync(from.CustomerId, to.AccountNumber.Value, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return new TransferResponse(tx.Id.Value, tx.Amount.Amount, tx.ReferenceId,
                                            from.Id.Value, to.Id.Value, tx.OccurredOn);
            }
            catch (ConcurrencyConflictException)
            {
                unitOfWork.DetachAll();
            }
            catch (UniqueConstraintViolationException)
            {
                unitOfWork.DetachAll();
                var winner = await transactions.GetByReferenceIdAsync(idempotencyKey, cancellationToken);
                if (winner is not null)
                {
                    EnsureIdempotentMatch(winner, command, from.Id.Value, to.Id.Value);
                    return new TransferResponse(winner.Id.Value, winner.Amount.Amount, winner.ReferenceId,
                                                winner.SourceAccountId!.Value,
                                                winner.DestinationAccountId!.Value,
                                                winner.OccurredOn);
                }
            }
        }

        throw IdempotencyKeys.RetryExhausted();
    }

    private async Task SaveBeneficiaryIfNewAsync(CustomerId owner, string destinationNumber, CancellationToken ct)
    {
        var existing = await beneficiaries.GetByOwnerAndNumberAsync(owner.Value, destinationNumber, ct);
        if (existing is not null)
            return;

        // Resolve the real holder name so the saved entry shows "account number + full name".
        var holderName = await ResolveHolderNameAsync(destinationNumber, ct) ?? "Saved account";

        var beneficiary = Beneficiary.Create(owner, new AccountNumber(destinationNumber), holderName);
        await beneficiaries.AddAsync(beneficiary, ct);
    }

    private async Task<string?> ResolveHolderNameAsync(string destinationNumber, CancellationToken ct)
    {
        const string sql = """
            SELECT c.full_name
            FROM   accounts a
            JOIN   customers c ON c.customer_id = a.customer_id
            WHERE  UPPER(a.account_number) = UPPER(@AccountNumber)
            """;

        using var connection = connectionFactory.CreateOpenConnection();
        return await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(sql, new { AccountNumber = destinationNumber }, cancellationToken: ct));
    }

    private static void EnsureIdempotentMatch(Transaction existing, TransferByAccountNumberCommand command, Guid fromId, Guid toId)
    {
        if (existing.Type != TransactionType.Transfer
            || existing.Amount.Amount != command.Amount
            || existing.SourceAccountId?.Value != fromId
            || existing.DestinationAccountId?.Value != toId)
            throw IdempotencyKeys.Mismatch("Idempotency key was already used with a different amount or accounts.");
    }
}

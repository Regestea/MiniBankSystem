using MiniBank.Domain.TransactionAggregate.ValueObjects;

namespace MiniBank.Domain.TransactionAggregate;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(TransactionId id, CancellationToken cancellationToken = default);
    /// <summary>Global lookup by idempotency key (ux_transactions_reference is GLOBAL on reference_id).</summary>
    Task<Transaction?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
}

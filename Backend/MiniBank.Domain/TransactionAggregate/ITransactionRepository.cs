using MiniBank.Domain.TransactionAggregate.ValueObjects;

namespace MiniBank.Domain.TransactionAggregate;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(TransactionId id, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
}

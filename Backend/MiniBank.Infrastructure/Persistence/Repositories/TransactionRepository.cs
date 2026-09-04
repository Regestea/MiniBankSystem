using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Domain.TransactionAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Repositories;

internal sealed class TransactionRepository(MiniBankDbContext db) : ITransactionRepository
{
    public Task<Transaction?> GetByIdAsync(TransactionId id, CancellationToken cancellationToken = default)
        => db.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Transaction?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        // ux_transactions_reference is GLOBAL on reference_id, so the lookup is global (trimmed).
        var normalized = referenceId.Trim();
        return db.Transactions.FirstOrDefaultAsync(
            t => t.ReferenceId == normalized,
            cancellationToken);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
        => await db.Transactions.AddAsync(transaction, cancellationToken);
}

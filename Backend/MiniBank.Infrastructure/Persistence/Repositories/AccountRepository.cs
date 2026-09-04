using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Repositories;

internal sealed class AccountRepository(MiniBankDbContext db) : IAccountRepository
{
    // Balance is now persisted in balance_amount column — O(1) read/write.
    // Ledger is only loaded when explicitly needed (statements).
    public Task<Account?> LoadAsync(AccountId id, CancellationToken cancellationToken = default)
        => db.Accounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
        => await db.Accounts.AddAsync(account, cancellationToken);
}

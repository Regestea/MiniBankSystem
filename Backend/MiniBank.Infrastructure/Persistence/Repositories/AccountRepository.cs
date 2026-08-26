using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Repositories;

internal sealed class AccountRepository(MiniBankDbContext db) : IAccountRepository
{
    public Task<Account?> LoadAsync(AccountId id, CancellationToken cancellationToken = default)
        => db.Accounts
            .Include(a => a.Ledger)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
        => await db.Accounts.AddAsync(account, cancellationToken);
}

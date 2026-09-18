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

    public Task<Account?> LoadByNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        // Normalize the same way AccountNumber does (trim + upper-case) so
        // "ir-123..." matches stored "IR-123...". Invalid formats return null
        // (callers map to 404) instead of throwing — validation lives in the
        // Features validators which return 400 with a clear message.
        var normalized = AccountNumber.Normalize(accountNumber);
        if (!AccountNumber.IsValid(normalized))
            return Task.FromResult<Account?>(null);

        var number = new AccountNumber(normalized);
        return db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == number, cancellationToken);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
        => await db.Accounts.AddAsync(account, cancellationToken);
}

using MiniBank.Domain.AccountAggregate.ValueObjects;

namespace MiniBank.Domain.AccountAggregate;

public interface IAccountRepository
{
    /// <summary>Loads the aggregate together with its append-only ledger (rehydrated state).</summary>
    Task<Account?> LoadAsync(AccountId id, CancellationToken cancellationToken = default);

    Task AddAsync(Account account, CancellationToken cancellationToken = default);
}

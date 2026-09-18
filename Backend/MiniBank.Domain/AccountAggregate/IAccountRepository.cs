using MiniBank.Domain.AccountAggregate.ValueObjects;

namespace MiniBank.Domain.AccountAggregate;

public interface IAccountRepository
{
    Task<Account?> LoadAsync(AccountId id, CancellationToken cancellationToken = default);

    /// <summary>Loads an account by its number (IR-XXXXXXXXXX or 16-digit, case-insensitive).</summary>
    Task<Account?> LoadByNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    Task AddAsync(Account account, CancellationToken cancellationToken = default);
}

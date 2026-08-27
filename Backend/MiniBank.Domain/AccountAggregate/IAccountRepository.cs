using MiniBank.Domain.AccountAggregate.ValueObjects;

namespace MiniBank.Domain.AccountAggregate;

public interface IAccountRepository
{
    Task<Account?> LoadAsync(AccountId id, CancellationToken cancellationToken = default);

    Task AddAsync(Account account, CancellationToken cancellationToken = default);
}

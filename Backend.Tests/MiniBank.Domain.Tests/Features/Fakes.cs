using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Domain.TransactionAggregate.ValueObjects;

namespace MiniBank.Domain.Tests.Features;

internal sealed class FakeCustomerRepository : ICustomerRepository
{
    public Dictionary<Guid, Customer> Store { get; } = new();
    public HashSet<string> ExistingEmails { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
        => Task.FromResult(Store.TryGetValue(id.Value, out var c) ? c : null);

    public Task AddAsync(Customer customer, CancellationToken ct = default)
    {
        Store[customer.Id.Value] = customer;
        ExistingEmails.Add(customer.Email);
        return Task.CompletedTask;
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => Task.FromResult(ExistingEmails.Contains(email));
}

internal sealed class FakeAccountRepository : IAccountRepository
{
    public Dictionary<Guid, Account> Store { get; } = new();

    public Task<Account?> LoadAsync(AccountId id, CancellationToken ct = default)
        => Task.FromResult(Store.TryGetValue(id.Value, out var a) ? a : null);

    public Task AddAsync(Account account, CancellationToken ct = default)
    {
        Store[account.Id.Value] = account;
        return Task.CompletedTask;
    }
}

internal sealed class FakeTransactionRepository : ITransactionRepository
{
    public List<Transaction> Store { get; } = new();

    public Task<Transaction?> GetByIdAsync(TransactionId id, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(t => t.Id == id));

    public Task AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        Store.Add(transaction);
        return Task.CompletedTask;
    }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCount++;
        return Task.FromResult(SaveCount);
    }
}

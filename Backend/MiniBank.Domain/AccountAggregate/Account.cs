using MiniBank.Domain.AccountAggregate.Events;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Domain.Ledger;
using MiniBank.Domain.TransactionAggregate.Events;

namespace MiniBank.Domain.AccountAggregate;

public sealed class Account : AggregateRoot<AccountId>
{
    public CustomerId CustomerId { get; private set; } = null!;
    public AccountNumber AccountNumber { get; private set; } = null!;
    public AccountType AccountType { get; private set; }
    public AccountStatus Status { get; private set; }

    private readonly List<LedgerEntry> _ledger = new();

    public IReadOnlyCollection<LedgerEntry> Ledger => _ledger.AsReadOnly();

    public Money Balance => CalculateOrderedBalance();

    private Account() { }

    private Account(AccountId id, AccountNumber accountNumber, CustomerId customerId, AccountType accountType)
        : base(id)
    {
        CustomerId = customerId;
        AccountNumber = accountNumber;
        AccountType = accountType;
        Status = AccountStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new AccountOpenedEvent(id, accountNumber, customerId, accountType));
    }

    public static Account Open(CustomerId customerId, AccountType accountType, AccountNumber? accountNumber = null, AccountId? id = null)
    {
        if (customerId is null)
            throw new DomainValidationException(nameof(customerId), "CustomerId cannot be null.");

        id ??= new AccountId(Guid.NewGuid());
        accountNumber ??= AccountNumber.Generate();

        return new Account(id, accountNumber, customerId, accountType);
    }

    /// <summary>Deposits money — creates transaction and appends posting.</summary>
    public (TransactionAggregate.Transaction Transaction, LedgerEntry Entry) Deposit(Money amount, string? description = null, string? referenceId = null)
    {
        EnsureActive();

        var tx = TransactionAggregate.Transaction.CreateDeposit(Id, amount, description, referenceId);
        var entry = AppendPosting(tx.ToLedgerEntries()[0]);

        AddDomainEvent(new MoneyDepositedEvent(Id, amount, entry.Id));
        return (tx, entry);
    }

    /// <summary>Withdraws money — validates funds and appends posting.</summary>
    public (TransactionAggregate.Transaction Transaction, LedgerEntry Entry) Withdraw(Money amount, string? referenceId = null, string? description = null)
    {
        EnsureActive();

        var tx = TransactionAggregate.Transaction.CreateWithdraw(Id, amount, Balance, description, referenceId);
        var entry = AppendPosting(tx.ToLedgerEntries()[0]);

        AddDomainEvent(new MoneyWithdrawnEvent(Id, amount, entry.Id));
        return (tx, entry);
    }

    /// <summary>Transfers money to another account via double-entry transaction.</summary>
    public TransactionAggregate.Transaction TransferTo(Account destination, Money amount, string? description = null, string? referenceId = null)
    {
        if (destination is null)
            throw new DomainValidationException(nameof(destination), "Destination account cannot be null.");
        if (destination.Id.Equals(Id))
            throw new DomainValidationException(nameof(destination), "Cannot transfer to the same account.");

        EnsureActive();
        destination.EnsureActive();

        var tx = TransactionAggregate.Transaction.CreateTransfer(Id, destination.Id, amount, Balance, description, referenceId);
        var (fromEntry, toEntry) = tx.ToTransferEntries();

        _ledger.Add(fromEntry);
        destination._ledger.Add(toEntry);

        IncrementVersion();
        destination.IncrementVersion();

        AddDomainEvent(new MoneyTransferredEvent(Id, destination.Id, amount, tx.ReferenceId, fromEntry.Id, toEntry.Id));

        return tx;
    }

    public void Freeze()
    {
        if (Status == AccountStatus.Frozen)
            throw new DomainOperationNotAllowedException(nameof(Status), "Account already frozen.");
        if (Status == AccountStatus.Closed)
            throw new DomainOperationNotAllowedException(nameof(Status), "Closed account cannot be frozen.");

        Status = AccountStatus.Frozen;
        IncrementVersion();
        AddDomainEvent(new AccountFrozenEvent(Id));
    }

    public void Unfreeze()
    {
        if (Status != AccountStatus.Frozen)
            throw new DomainOperationNotAllowedException(nameof(Status), "Only frozen account can be unfrozen.");

        Status = AccountStatus.Active;
        IncrementVersion();
        AddDomainEvent(new AccountUnfrozenEvent(Id));
    }

    public void Close()
    {
        if (Status == AccountStatus.Closed)
            throw new DomainOperationNotAllowedException(nameof(Status), "Account already closed.");
        if (Status == AccountStatus.Frozen)
            throw new DomainOperationNotAllowedException(nameof(Status), "Frozen account cannot be closed. Unfreeze first.");

        if (!Balance.IsZero)
            throw new DomainInvariantViolationException(nameof(Balance), $"Cannot close account with non-zero balance: {Balance.Amount}");

        Status = AccountStatus.Closed;
        IncrementVersion();
        AddDomainEvent(new AccountClosedEvent(Id));
    }

    public IReadOnlyList<LedgerEntry> GetStatementOrdered()
        => _ledger.OrderBy(e => e.OccurredOn).ThenBy(e => e.Id).ToList().AsReadOnly();

    private LedgerEntry AppendPosting(LedgerEntry entry)
    {
        _ledger.Add(entry);
        IncrementVersion();
        return entry;
    }

    private Money CalculateOrderedBalance()
    {
        decimal total = 0m;
        foreach (var entry in _ledger.OrderBy(e => e.OccurredOn).ThenBy(e => e.Id))
        {
            switch (entry.Type)
            {
                case LedgerEntryType.Deposit:
                case LedgerEntryType.TransferIn:
                    total += entry.Amount.Amount;
                    break;
                case LedgerEntryType.Withdraw:
                case LedgerEntryType.TransferOut:
                    total -= entry.Amount.Amount;
                    break;
            }
        }

        return Money.FromDecimal(total);
    }

    private void EnsureActive()
    {
        if (Status != AccountStatus.Active)
            throw new DomainOperationNotAllowedException(nameof(Status), $"Account is {Status}, operation not allowed. Only Active accounts can transact.");
    }

    private Account(AccountId id, AccountNumber accountNumber, CustomerId customerId, AccountType accountType, AccountStatus status, List<LedgerEntry> ledger, int version, DateTimeOffset createdAt, DateTimeOffset updatedAt)
        : base(id)
    {
        AccountNumber = accountNumber;
        CustomerId = customerId;
        AccountType = accountType;
        Status = status;
        _ledger = ledger ?? new();
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Account Rehydrate(AccountId id, AccountNumber accountNumber, CustomerId customerId, AccountType accountType, AccountStatus status, IEnumerable<LedgerEntry> ledger, int version, DateTimeOffset createdAt, DateTimeOffset updatedAt)
        => new(id, accountNumber, customerId, accountType, status, ledger.ToList(), version, createdAt, updatedAt);
}

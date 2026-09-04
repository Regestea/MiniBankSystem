using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.Events;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Domain.Ledger;
using MiniBank.Domain.TransactionAggregate.Events;

namespace MiniBank.Domain.Tests.AccountAggregate;

public class AccountTests
{
    private static Account CreateAccount(CustomerId? customerId = null, AccountType type = AccountType.Current)
    {
        customerId ??= new CustomerId(Guid.NewGuid());
        var account = Account.Open(customerId, type);
        account.Approve();
        return account;
    }

    [Fact]
    public void Open_Valid_CreatesPendingApprovalAccountWithEvent()
    {
        var customerId = new CustomerId(Guid.NewGuid());
        var account = Account.Open(customerId, AccountType.Savings);
        Assert.Equal(customerId, account.CustomerId);
        Assert.Equal(AccountType.Savings, account.AccountType);
        Assert.Equal(AccountStatus.PendingApproval, account.Status);
        Assert.Equal(0, account.Version);
        Assert.Single(account.DomainEvents);
        Assert.IsType<AccountOpenedEvent>(account.DomainEvents.First());
        Assert.NotNull(account.AccountNumber);
        Assert.Equal(0m, account.Balance.Amount);
    }

    [Fact]
    public void Open_NullCustomerId_ThrowsDomainValidation()
    {
        Assert.Throws<DomainValidationException>(() => Account.Open(null!, AccountType.Current));
    }

    [Fact]
    public void Open_WithSpecificIdAndNumber_UsesProvidedValues()
    {
        var id = new AccountId(Guid.NewGuid());
        var number = new AccountNumber("1234567890123456");
        var customerId = new CustomerId(Guid.NewGuid());
        var account = Account.Open(customerId, AccountType.Current, number, id);
        Assert.Equal(id, account.Id);
        Assert.Equal(number, account.AccountNumber);
    }

    [Fact]
    public void Deposit_Valid_AddsLedgerEntryAndEventAndIncrementsBalance()
    {
        var account = CreateAccount();
        account.ClearDomainEvents();
        var (tx, entry) = account.Deposit(Money.FromDecimal(1000m));
        Assert.Single(account.Ledger);
        Assert.Equal(1000m, account.Balance.Amount);
        Assert.Equal(LedgerEntryType.Deposit, entry.Type);
        // Transaction journal returned — not orphaned; carries its own events
        Assert.NotNull(tx);
        Assert.NotEmpty(tx.DomainEvents);
        Assert.Equal(entry.ReferenceId, tx.ReferenceId);
        Assert.Equal(2, account.Version);
        Assert.Single(account.DomainEvents);
        Assert.IsType<MoneyDepositedEvent>(account.DomainEvents.First());
    }

    [Fact]
    public void Deposit_ZeroAmount_ThrowsDomainValidation()
    {
        var account = CreateAccount();
        Assert.Throws<DomainValidationException>(() => account.Deposit(Money.Zero));
    }

    [Fact]
    public void Deposit_WhenFrozen_ThrowsDomainOperationNotAllowed()
    {
        var account = CreateAccount();
        account.Freeze();
        Assert.Throws<DomainOperationNotAllowedException>(() => account.Deposit(Money.FromDecimal(100m)));
    }

    [Fact]
    public void Deposit_WhenClosed_ThrowsDomainOperationNotAllowed()
    {
        var account = CreateAccount();
        account.Close(); // zero balance, so can close
        Assert.Throws<DomainOperationNotAllowedException>(() => account.Deposit(Money.FromDecimal(100m)));
    }

    [Fact]
    public void Withdraw_Valid_DecrementsBalanceAndCreatesEntry()
    {
        var account = CreateAccount();
        account.Deposit(Money.FromDecimal(1000m));
        account.ClearDomainEvents();
        var (_, entry) = account.Withdraw(Money.FromDecimal(300m));
        Assert.Equal(700m, account.Balance.Amount);
        Assert.Equal(LedgerEntryType.Withdraw, entry.Type);
        Assert.IsType<MoneyWithdrawnEvent>(account.DomainEvents.First());
    }

    [Fact]
    public void Withdraw_ZeroAmount_ThrowsDomainValidation()
    {
        var account = CreateAccount();
        account.Deposit(Money.FromDecimal(500m));
        Assert.Throws<DomainValidationException>(() => account.Withdraw(Money.Zero));
    }

    [Fact]
    public void Withdraw_InsufficientFunds_ThrowsDomainInvariantViolation()
    {
        var account = CreateAccount();
        account.Deposit(Money.FromDecimal(100m));
        var ex = Assert.Throws<DomainInvariantViolationException>(() => account.Withdraw(Money.FromDecimal(200m)));
        Assert.Contains("Insufficient funds", ex.Details.ToString());
    }

    [Fact]
    public void Withdraw_WhenFrozen_ThrowsDomainOperationNotAllowed()
    {
        var account = CreateAccount();
        account.Deposit(Money.FromDecimal(500m));
        account.Freeze();
        Assert.Throws<DomainOperationNotAllowedException>(() => account.Withdraw(Money.FromDecimal(100m)));
    }

    [Fact]
    public void Freeze_FromActive_SucceedsAndRaisesEvent()
    {
        var account = CreateAccount();
        account.ClearDomainEvents();
        account.Freeze();
        Assert.Equal(AccountStatus.Frozen, account.Status);
        Assert.IsType<AccountFrozenEvent>(account.DomainEvents.First());
    }

    [Fact]
    public void Freeze_AlreadyFrozen_Throws()
    {
        var account = CreateAccount();
        account.Freeze();
        Assert.Throws<DomainOperationNotAllowedException>(() => account.Freeze());
    }

    [Fact]
    public void Freeze_WhenClosed_Throws()
    {
        var account = CreateAccount();
        account.Close();
        Assert.Throws<DomainOperationNotAllowedException>(() => account.Freeze());
    }

    [Fact]
    public void Unfreeze_FromFrozen_Succeeds()
    {
        var account = CreateAccount();
        account.Freeze();
        account.ClearDomainEvents();
        account.Unfreeze();
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.IsType<AccountUnfrozenEvent>(account.DomainEvents.First());
    }

    [Fact]
    public void Unfreeze_NotFrozen_Throws()
    {
        var account = CreateAccount();
        Assert.Throws<DomainOperationNotAllowedException>(() => account.Unfreeze());
    }

    [Fact]
    public void Close_WithZeroBalance_Succeeds()
    {
        var account = CreateAccount();
        account.ClearDomainEvents();
        account.Close();
        Assert.Equal(AccountStatus.Closed, account.Status);
        Assert.IsType<AccountClosedEvent>(account.DomainEvents.First());
    }

    [Fact]
    public void Close_WithNonZeroBalance_ThrowsDomainInvariantViolation()
    {
        var account = CreateAccount();
        account.Deposit(Money.FromDecimal(100m));
        var ex = Assert.Throws<DomainInvariantViolationException>(() => account.Close());
        Assert.Contains("non-zero balance", ex.Details.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Close_AlreadyClosed_Throws()
    {
        var account = CreateAccount();
        account.Close();
        Assert.Throws<DomainOperationNotAllowedException>(() => account.Close());
    }

    [Fact]
    public void Close_FromFrozen_ThrowsDomainOperationNotAllowed()
    {
        var account = CreateAccount();
        account.Freeze();
        var ex = Assert.Throws<DomainOperationNotAllowedException>(() => account.Close());
        Assert.Contains("Unfreeze", ex.Details.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetStatementOrdered_ReturnsChronological()
    {
        var account = CreateAccount();
        account.Deposit(Money.FromDecimal(1000m));
        // Small delay to ensure distinct timestamps (or use same timestamp ordering is insertion order)
        account.Withdraw(Money.FromDecimal(200m));
        account.Deposit(Money.FromDecimal(500m));
        var statement = account.GetStatementOrdered();
        Assert.Equal(3, statement.Count);
        // Should be in order of creation
        Assert.Equal(LedgerEntryType.Deposit, statement[0].Type);
        Assert.Equal(LedgerEntryType.Withdraw, statement[1].Type);
        Assert.Equal(LedgerEntryType.Deposit, statement[2].Type);
        // Balance should be 1300
        Assert.Equal(1300m, account.Balance.Amount);
    }

    [Fact]
    public void Balance_Calculation_ConsidersAllTransactionTypes()
    {
        var account = CreateAccount();
        account.Deposit(Money.FromDecimal(1000m));
        account.Deposit(Money.FromDecimal(500m));
        account.Withdraw(Money.FromDecimal(200m));
        // Simulate transfer via service? For direct, we test ledger entry types
        // TransferOut and TransferIn via internal methods — test via service in service tests
        Assert.Equal(1300m, account.Balance.Amount);
    }

    [Fact]
    public void Rehydrate_RestoresStateCorrectly()
    {
        var id = new AccountId(Guid.NewGuid());
        var number = new AccountNumber("1234567890123456");
        var customerId = new CustomerId(Guid.NewGuid());
        var ledger = new List<LedgerEntry>
        {
            LedgerEntry.CreateDeposit(id, Money.FromDecimal(1000m)),
            LedgerEntry.CreateWithdraw(id, Money.FromDecimal(400m))
        };
        var account = Account.Rehydrate(id, number, customerId, AccountType.Current, AccountStatus.Active, ledger, 2, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);
        Assert.Equal(id, account.Id);
        Assert.Equal(number, account.AccountNumber);
        Assert.Equal(600m, account.Balance.Amount);
        Assert.Equal(2, account.Version);
        Assert.Equal(2, account.Ledger.Count);
    }

    [Fact]
    public void ClearDomainEvents_Clears()
    {
        var account = CreateAccount();
        Assert.NotEmpty(account.DomainEvents);
        account.ClearDomainEvents();
        Assert.Empty(account.DomainEvents);
    }

    [Fact]
    public void Version_IncrementsOnEachOperation()
    {
        var account = CreateAccount(); // Open + Approve = version 1
        Assert.Equal(1, account.Version);
        account.Deposit(Money.FromDecimal(100m));
        Assert.Equal(2, account.Version);
        account.Withdraw(Money.FromDecimal(50m));
        Assert.Equal(3, account.Version);
        account.Freeze();
        Assert.Equal(4, account.Version);
    }
}

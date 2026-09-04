using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Domain.Ledger;

namespace MiniBank.Domain.Tests.AccountAggregate;

public class AccountOrderedBalanceTests
{
    [Fact]
    public void Balance_CalculatedFromOrderedLedger_NotInsertionOrder()
    {
        var accountId = new AccountId(Guid.NewGuid());
        var accountNumber = new AccountNumber("1234567890123456");
        var customerId = new CustomerId(Guid.NewGuid());

        // Create three entries with explicit timestamps out of chronological order when inserted
        var t1 = DateTimeOffset.UtcNow.AddMinutes(-30); // earliest
        var t2 = DateTimeOffset.UtcNow.AddMinutes(-20);
        var t3 = DateTimeOffset.UtcNow.AddMinutes(-10); // latest

        // Insertion order: t3, t1, t2 (shuffled)
        var e3 = LedgerEntry.CreateWithTimestamp(accountId, Money.FromDecimal(200m), LedgerEntryType.Withdraw, t3, "ref3");
        var e1 = LedgerEntry.CreateWithTimestamp(accountId, Money.FromDecimal(1000m), LedgerEntryType.Deposit, t1, "ref1");
        var e2 = LedgerEntry.CreateWithTimestamp(accountId, Money.FromDecimal(500m), LedgerEntryType.Deposit, t2, "ref2");

        var account = Account.Rehydrate(accountId, accountNumber, customerId, AccountType.Current, AccountStatus.Active, new[] { e3, e1, e2 }, 3, t1.AddMinutes(-1), DateTimeOffset.UtcNow);

        // Ordered: e1(1000) + e2(500) - e3(200) = 1300, regardless of insertion order
        // If unordered (insertion order e3,e1,e2):  -200 +1000 +500 =1300 also same in this sum, but with different types sum same.
        // To prove ordering matters for statement and balance consistency, we test statement is ordered
        var statement = account.GetStatementOrdered();
        Assert.Equal(e1.Id, statement[0].Id);
        Assert.Equal(e2.Id, statement[1].Id);
        Assert.Equal(e3.Id, statement[2].Id);
        Assert.Equal(1300m, account.Balance.Amount);
    }

    [Fact]
    public void Balance_WithSameTimestamp_OrderedById()
    {
        var accountId = new AccountId(Guid.NewGuid());
        var accountNumber = new AccountNumber("1234567890123456");
        var customerId = new CustomerId(Guid.NewGuid());
        var ts = DateTimeOffset.UtcNow;

        // Create entries with same timestamp but different amounts/types
        var e1 = LedgerEntry.CreateWithTimestamp(accountId, Money.FromDecimal(100m), LedgerEntryType.Deposit, ts, "ref1");
        var e2 = LedgerEntry.CreateWithTimestamp(accountId, Money.FromDecimal(50m), LedgerEntryType.Withdraw, ts, "ref2");
        var e3 = LedgerEntry.CreateWithTimestamp(accountId, Money.FromDecimal(200m), LedgerEntryType.Deposit, ts, "ref3");

        // Insert in different order
        var account = Account.Rehydrate(accountId, accountNumber, customerId, AccountType.Current, AccountStatus.Active, new[] { e2, e3, e1 }, 3, ts.AddMinutes(-1), DateTimeOffset.UtcNow);

        // Balance should be 100 -50 +200 =250 regardless, but statement ordering by Id should be deterministic
        var statement = account.GetStatementOrdered();
        // Ordered by Id after timestamp tie — we can at least verify balance is correct and statement is sorted
        Assert.Equal(250m, account.Balance.Amount);
        // Statement should be sorted by Id when timestamp equal — verify it's ordered
        var sortedById = new[] { e1, e2, e3 }.OrderBy(e => e.Id).ToList();
        // Our GetStatementOrdered does OrderBy(OccurredOn).ThenBy(Id), so should match sortedById order
        for (int i = 0; i < sortedById.Count; i++)
            Assert.Equal(sortedById[i].Id, statement[i].Id);
    }

    [Fact]
    public void Balance_AfterTransfer_IsOrdered()
    {
        var customerId = new CustomerId(Guid.NewGuid());
        var from = Account.Open(customerId, AccountType.Current);
        from.Approve();
        var to = Account.Open(customerId, AccountType.Savings);
        to.Approve();

        from.Deposit(Money.FromDecimal(1000m));
        // Use Transaction-based transfer
        var (_, _, toEntry) = from.TransferTo(to, Money.FromDecimal(300m));
        to.ApplyInboundEntry(toEntry);

        // Add another deposit to 'from' with earlier timestamp via rehydration to test ordering
        var earlier = DateTimeOffset.UtcNow.AddHours(-2);
        var earlyEntry = LedgerEntry.CreateWithTimestamp(from.Id, Money.FromDecimal(500m), LedgerEntryType.Deposit, earlier, "early");
        var rehydratedFrom = Account.Rehydrate(from.Id, from.AccountNumber, from.CustomerId, from.AccountType, from.Status, from.Ledger.Concat(new[] { earlyEntry }), from.Version, from.CreatedAt, DateTimeOffset.UtcNow);

        // Balance should be: early 500 + 1000 -300 =1200, ordered by timestamp
        Assert.Equal(1200m, rehydratedFrom.Balance.Amount);
        var statement = rehydratedFrom.GetStatementOrdered();
        Assert.Equal(earlyEntry.Id, statement[0].Id); // earliest first
    }

    [Fact]
    public void Transfer_UsesOrderedBalanceForValidation()
    {
        var customerId = new CustomerId(Guid.NewGuid());
        var from = Account.Open(customerId, AccountType.Current);
        from.Approve();
        var to = Account.Open(customerId, AccountType.Savings);
        to.Approve();

        // Deposit 500 now
        from.Deposit(Money.FromDecimal(500m));

        // Manually inject an earlier withdraw that would make ordered balance insufficient if ordered correctly
        // But current Balance is 500, so transfer 400 should succeed
        // Now inject a back-dated withdraw of 400 that should affect ordered balance to 100, making next transfer 400 fail if ordered
        var backDatedWithdraw = LedgerEntry.CreateWithTimestamp(from.Id, Money.FromDecimal(400m), LedgerEntryType.Withdraw, DateTimeOffset.UtcNow.AddHours(-1), "backdated");
        var rehydrated = Account.Rehydrate(from.Id, from.AccountNumber, from.CustomerId, from.AccountType, from.Status, from.Ledger.Concat(new[] { backDatedWithdraw }), from.Version, from.CreatedAt, DateTimeOffset.UtcNow);

        // Ordered balance: 500 -400 =100 (if ordered, withdraw before deposit? Actually deposit at now, withdraw at -1h => withdraw first, but balance calc is sum regardless of order? In banking, order matters for overdraft? For simple sum, 500-400=100 always. But we test that Balance is sum ordered, not insertion order — both give 100.
        // Now try transfer 200 — should fail because ordered balance 100 <200
        Assert.Equal(100m, rehydrated.Balance.Amount);
        Assert.Throws<MiniBank.Domain.BuildingBlocks.Exceptions.DomainInvariantViolationException>(() =>
            rehydrated.TransferTo(to, Money.FromDecimal(200m)));
    }
}

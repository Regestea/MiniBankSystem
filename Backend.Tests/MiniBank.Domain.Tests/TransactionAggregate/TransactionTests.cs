using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.Ledger;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Domain.TransactionAggregate.Events;

namespace MiniBank.Domain.Tests.TransactionAggregate;

public class TransactionTests
{
    private static AccountId ValidAccountId => new AccountId(Guid.NewGuid());

    [Fact]
    public void CreateDeposit_Valid_CreatesTransactionWithPosting()
    {
        var accountId = ValidAccountId;
        var amount = Money.FromDecimal(100m);
        var tx = Transaction.CreateDeposit(accountId, amount);

        Assert.Equal(MiniBank.Domain.TransactionAggregate.TransactionType.Deposit, tx.Type);
        Assert.Equal(amount, tx.Amount);
        Assert.Null(tx.SourceAccountId);
        Assert.Equal(accountId, tx.DestinationAccountId);
        Assert.Equal(tx.Id.Value.ToString("N"), tx.ReferenceId);
        Assert.Single(tx.Postings);
        Assert.Equal(LedgerEntryType.Deposit, tx.Postings[0].Type);
        Assert.Equal(amount, tx.Postings[0].Amount);
        Assert.Equal(accountId, tx.Postings[0].AccountId);
        Assert.Equal(tx.ReferenceId, tx.Postings[0].ReferenceId);
        Assert.Equal(tx.OccurredOn, tx.Postings[0].OccurredOn);
        Assert.Single(tx.DomainEvents);
        Assert.IsType<TransactionCreatedEvent>(tx.DomainEvents.First());
    }

    [Fact]
    public void CreateDeposit_ZeroAmount_ThrowsDomainValidation()
    {
        var accountId = ValidAccountId;
        Assert.Throws<DomainValidationException>(() => Transaction.CreateDeposit(accountId, Money.Zero));
    }

    [Fact]
    public void CreateDeposit_NullAccountId_ThrowsDomainValidation()
    {
        Assert.Throws<DomainValidationException>(() => Transaction.CreateDeposit(null!, Money.FromDecimal(100m)));
    }

    [Fact]
    public void CreateWithdraw_Valid_CreatesTransactionWithPosting()
    {
        var accountId = ValidAccountId;
        var amount = Money.FromDecimal(50m);
        var balance = Money.FromDecimal(100m);
        var tx = Transaction.CreateWithdraw(accountId, amount, balance);

        Assert.Equal(MiniBank.Domain.TransactionAggregate.TransactionType.Withdraw, tx.Type);
        Assert.Equal(accountId, tx.SourceAccountId);
        Assert.Null(tx.DestinationAccountId);
        Assert.Single(tx.Postings);
        Assert.Equal(LedgerEntryType.Withdraw, tx.Postings[0].Type);
        Assert.Equal(amount, tx.Postings[0].Amount);
    }

    [Fact]
    public void CreateWithdraw_InsufficientFunds_ThrowsDomainInvariantViolation()
    {
        var accountId = ValidAccountId;
        var balance = Money.FromDecimal(50m);
        var ex = Assert.Throws<DomainInvariantViolationException>(() =>
            Transaction.CreateWithdraw(accountId, Money.FromDecimal(100m), balance));
        Assert.Contains("Insufficient funds", ex.Details.ToString());
    }

    [Fact]
    public void CreateWithdraw_ZeroAmount_ThrowsDomainValidation()
    {
        var accountId = ValidAccountId;
        Assert.Throws<DomainValidationException>(() => Transaction.CreateWithdraw(accountId, Money.Zero, Money.FromDecimal(100m)));
    }

    [Fact]
    public void CreateTransfer_Valid_CreatesDoubleEntryWithSameReferenceAndTimestamp()
    {
        var from = ValidAccountId;
        var to = new AccountId(Guid.NewGuid());
        var amount = Money.FromDecimal(200m);
        var balance = Money.FromDecimal(1000m);
        var tx = Transaction.CreateTransfer(from, to, amount, balance);

        Assert.Equal(MiniBank.Domain.TransactionAggregate.TransactionType.Transfer, tx.Type);
        Assert.Equal(from, tx.SourceAccountId);
        Assert.Equal(to, tx.DestinationAccountId);
        Assert.Equal(2, tx.Postings.Count);
        Assert.Equal(2, tx.ToLedgerEntries().Count);

        var fromEntry = tx.Postings[0];
        var toEntry = tx.Postings[1];
        Assert.Equal(LedgerEntryType.TransferOut, fromEntry.Type);
        Assert.Equal(LedgerEntryType.TransferIn, toEntry.Type);
        Assert.Equal(amount, fromEntry.Amount);
        Assert.Equal(amount, toEntry.Amount);
        Assert.Equal(from, fromEntry.AccountId);
        Assert.Equal(to, toEntry.AccountId);
        // Same ReferenceId and OccurredOn — double-entry, ordered
        Assert.Equal(tx.ReferenceId, fromEntry.ReferenceId);
        Assert.Equal(tx.ReferenceId, toEntry.ReferenceId);
        Assert.Equal(tx.OccurredOn, fromEntry.OccurredOn);
        Assert.Equal(tx.OccurredOn, toEntry.OccurredOn);
        // Not same Id
        Assert.NotEqual(fromEntry.Id, toEntry.Id);
        // Domain events — MoneyTransferredEvent is emitted by Account.TransferTo (not duplicated here)
        Assert.Contains(tx.DomainEvents, e => e is TransactionCreatedEvent);
        Assert.DoesNotContain(tx.DomainEvents, e => e is MiniBank.Domain.TransactionAggregate.Events.MoneyTransferredEvent);
    }

    [Fact]
    public void CreateTransfer_SameAccount_ThrowsDomainValidation()
    {
        var id = ValidAccountId;
        Assert.Throws<DomainValidationException>(() =>
            Transaction.CreateTransfer(id, id, Money.FromDecimal(100m), Money.FromDecimal(1000m)));
    }

    [Fact]
    public void CreateTransfer_NullAccounts_ThrowsDomainValidation()
    {
        var valid = ValidAccountId;
        Assert.Throws<DomainValidationException>(() =>
            Transaction.CreateTransfer(null!, valid, Money.FromDecimal(100m), Money.FromDecimal(1000m)));
        Assert.Throws<DomainValidationException>(() =>
            Transaction.CreateTransfer(valid, null!, Money.FromDecimal(100m), Money.FromDecimal(1000m)));
    }

    [Fact]
    public void CreateTransfer_ZeroAmount_ThrowsDomainValidation()
    {
        var from = ValidAccountId;
        var to = new AccountId(Guid.NewGuid());
        Assert.Throws<DomainValidationException>(() =>
            Transaction.CreateTransfer(from, to, Money.Zero, Money.FromDecimal(1000m)));
    }

    [Fact]
    public void CreateTransfer_InsufficientFunds_ThrowsDomainInvariantViolation()
    {
        var from = ValidAccountId;
        var to = new AccountId(Guid.NewGuid());
        var balance = Money.FromDecimal(100m);
        Assert.Throws<DomainInvariantViolationException>(() =>
            Transaction.CreateTransfer(from, to, Money.FromDecimal(200m), balance));
    }

    [Fact]
    public void ToLedgerEntries_Cached_SameInstancesOnMultipleCalls()
    {
        var tx = Transaction.CreateDeposit(ValidAccountId, Money.FromDecimal(100m));
        var first = tx.ToLedgerEntries();
        var second = tx.ToLedgerEntries();
        // AsReadOnly creates new wrapper, but underlying entries are same instances
        Assert.NotSame(first, second);
        Assert.Equal(first[0].Id, second[0].Id);
        Assert.Same(first[0], second[0]);
    }

    [Fact]
    public void ToTransferEntries_OnlyForTransfer()
    {
        var deposit = Transaction.CreateDeposit(ValidAccountId, Money.FromDecimal(100m));
        Assert.Throws<DomainOperationNotAllowedException>(() => deposit.ToTransferEntries());

        var from = ValidAccountId;
        var to = new AccountId(Guid.NewGuid());
        var transfer = Transaction.CreateTransfer(from, to, Money.FromDecimal(50m), Money.FromDecimal(100m));
        var (outEntry, inEntry) = transfer.ToTransferEntries();
        Assert.Equal(from, outEntry.AccountId);
        Assert.Equal(to, inEntry.AccountId);
    }

    [Fact]
    public void Rehydrate_RestoresCorrectly()
    {
        var id = new MiniBank.Domain.TransactionAggregate.ValueObjects.TransactionId(Guid.NewGuid());
        var from = ValidAccountId;
        var to = new AccountId(Guid.NewGuid());
        var amount = Money.FromDecimal(123.45m);
        var occurredOn = DateTimeOffset.UtcNow.AddHours(-1);
        var tx = Transaction.Rehydrate(id, MiniBank.Domain.TransactionAggregate.TransactionType.Transfer, amount, from, to, occurredOn, id.Value.ToString("N"), 5);
        Assert.Equal(id, tx.Id);
        Assert.Equal(MiniBank.Domain.TransactionAggregate.TransactionType.Transfer, tx.Type);
        Assert.Equal(amount, tx.Amount);
        Assert.Equal(from, tx.SourceAccountId);
        Assert.Equal(to, tx.DestinationAccountId);
        Assert.Equal(occurredOn, tx.OccurredOn);
        Assert.Equal(5, tx.Version);
        Assert.Equal(2, tx.Postings.Count);
        Assert.Equal(occurredOn, tx.Postings[0].OccurredOn);
    }

    [Fact]
    public void Transaction_IsAggregateRoot_HasVersionAndEvents()
    {
        var tx = Transaction.CreateDeposit(ValidAccountId, Money.FromDecimal(10m));
        Assert.Equal(0, tx.Version);
        Assert.NotEmpty(tx.DomainEvents);
        tx.ClearDomainEvents();
        Assert.Empty(tx.DomainEvents);
    }

    [Fact]
    public void CreateDeposit_WithCustomReferenceId_ReferenceIdFlowsToPostings()
    {
        var accountId = ValidAccountId;
        var customRef = "CUSTOM-REF-123";
        var tx = Transaction.CreateDeposit(accountId, Money.FromDecimal(100m), referenceId: customRef);

        Assert.Equal(customRef, tx.ReferenceId);
        var posting = tx.ToLedgerEntries().Single();
        Assert.Equal(customRef, posting.ReferenceId);
    }

    [Fact]
    public void CreateTransfer_WithCustomReferenceId_ReferenceIdSharedByBothPostings()
    {
        var from = ValidAccountId;
        var to = new AccountId(Guid.NewGuid());
        var customRef = "TX-ABC-999";
        var tx = Transaction.CreateTransfer(from, to, Money.FromDecimal(50m), Money.FromDecimal(500m), referenceId: customRef);

        Assert.Equal(customRef, tx.ReferenceId);
        var (outEntry, inEntry) = tx.ToTransferEntries();
        Assert.Equal(customRef, outEntry.ReferenceId);
        Assert.Equal(customRef, inEntry.ReferenceId);
    }

    [Fact]
    public void Rehydrate_WithStoredPostings_PreservesOriginalPostingIds()
    {
        // Simulate persistence: postings stored with original Ids
        var id = new MiniBank.Domain.TransactionAggregate.ValueObjects.TransactionId(Guid.NewGuid());
        var accountId = ValidAccountId;
        var occurredOn = DateTimeOffset.UtcNow.AddHours(-2);
        var reference = id.Value.ToString("N");

        var storedPostings = new List<MiniBank.Domain.Ledger.LedgerEntry>
        {
            MiniBank.Domain.Ledger.LedgerEntry.CreateWithTimestamp(accountId, Money.FromDecimal(75m), MiniBank.Domain.Ledger.LedgerEntryType.Deposit, occurredOn, reference, "Deposit")
        };

        var tx = Transaction.Rehydrate(id, MiniBank.Domain.TransactionAggregate.TransactionType.Deposit,
            Money.FromDecimal(75m), null, accountId, occurredOn, reference, 3, storedPostings);

        // Original Id preserved — critical for event correlation after replay
        Assert.Single(tx.Postings);
        Assert.Equal(storedPostings[0].Id, tx.Postings[0].Id);
        Assert.Same(storedPostings[0], tx.Postings[0]);
        Assert.Equal(reference, tx.ReferenceId);
        Assert.Equal(3, tx.Version);
    }

    [Fact]
    public void Rehydrate_TransferWithStoredPostings_BothOriginalIdsPreserved()
    {
        var id = new MiniBank.Domain.TransactionAggregate.ValueObjects.TransactionId(Guid.NewGuid());
        var from = ValidAccountId;
        var to = new AccountId(Guid.NewGuid());
        var amount = Money.FromDecimal(200m);
        var occurredOn = DateTimeOffset.UtcNow.AddMinutes(-30);
        var reference = "stored-ref";

        var storedOut = MiniBank.Domain.Ledger.LedgerEntry.CreateWithTimestamp(from, amount, MiniBank.Domain.Ledger.LedgerEntryType.TransferOut, occurredOn, reference, "Transfer Out");
        var storedIn = MiniBank.Domain.Ledger.LedgerEntry.CreateWithTimestamp(to, amount, MiniBank.Domain.Ledger.LedgerEntryType.TransferIn, occurredOn, reference, "Transfer In");
        var originalIds = new[] { storedOut.Id, storedIn.Id };

        var tx = Transaction.Rehydrate(id, MiniBank.Domain.TransactionAggregate.TransactionType.Transfer, amount, from, to, occurredOn, reference, 1, new[] { storedOut, storedIn });

        Assert.Equal(originalIds[0], tx.ToLedgerEntries()[0].Id);
        Assert.Equal(originalIds[1], tx.ToLedgerEntries()[1].Id);
        var (outE, inE) = tx.ToTransferEntries();
        Assert.Equal(originalIds[0], outE.Id);
        Assert.Equal(originalIds[1], inE.Id);
    }
}

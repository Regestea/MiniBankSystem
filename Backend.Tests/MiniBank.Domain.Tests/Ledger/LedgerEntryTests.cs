using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.Ledger;

namespace MiniBank.Domain.Tests.Ledger;

public class LedgerEntryTests
{
    private static AccountId ValidAccountId => new AccountId(Guid.NewGuid());

    [Fact]
    public void Create_Valid_SetsProperties()
    {
        var accountId = ValidAccountId;
        var amount = Money.FromDecimal(100m);
        var entry = LedgerEntry.Create(accountId, amount, LedgerEntryType.Deposit, "ref1", "desc");
        Assert.Equal(accountId, entry.AccountId);
        Assert.Equal(amount, entry.Amount);
        Assert.Equal(LedgerEntryType.Deposit, entry.Type);
        Assert.Equal("ref1", entry.ReferenceId);
        Assert.Equal("desc", entry.Description);
        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.True((DateTimeOffset.UtcNow - entry.OccurredOn).TotalSeconds < 5);
    }

    [Fact]
    public void Create_ZeroAmount_ThrowsDomainValidationException()
    {
        var accountId = ValidAccountId;
        Assert.Throws<DomainValidationException>(() => LedgerEntry.Create(accountId, Money.Zero, LedgerEntryType.Deposit));
    }

    [Fact]
    public void CreateDeposit_CreatesDepositEntry()
    {
        var accountId = ValidAccountId;
        var amount = Money.FromDecimal(500m);
        var entry = LedgerEntry.CreateDeposit(accountId, amount, "ref-dep");
        Assert.Equal(LedgerEntryType.Deposit, entry.Type);
        Assert.Equal("Deposit", entry.Description);
        Assert.Equal("ref-dep", entry.ReferenceId);
    }

    [Fact]
    public void CreateWithdraw_CreatesWithdrawEntry()
    {
        var accountId = ValidAccountId;
        var amount = Money.FromDecimal(200m);
        var entry = LedgerEntry.CreateWithdraw(accountId, amount);
        Assert.Equal(LedgerEntryType.Withdraw, entry.Type);
        Assert.Equal("Withdraw", entry.Description);
    }

    [Fact]
    public void CreateTransferOut_CreatesTransferOutEntry()
    {
        var accountId = ValidAccountId;
        var amount = Money.FromDecimal(100m);
        var entry = LedgerEntry.CreateTransferOut(accountId, amount, "transfer-123");
        Assert.Equal(LedgerEntryType.TransferOut, entry.Type);
        Assert.Equal("transfer-123", entry.ReferenceId);
        Assert.Equal("Transfer Out", entry.Description);
    }

    [Fact]
    public void CreateTransferIn_CreatesTransferInEntry()
    {
        var accountId = ValidAccountId;
        var amount = Money.FromDecimal(100m);
        var entry = LedgerEntry.CreateTransferIn(accountId, amount, "transfer-123");
        Assert.Equal(LedgerEntryType.TransferIn, entry.Type);
        Assert.Equal("transfer-123", entry.ReferenceId);
        Assert.Equal("Transfer In", entry.Description);
    }

    [Fact]
    public void CreateTransferIn_ZeroAmount_Throws()
    {
        var accountId = ValidAccountId;
        Assert.Throws<DomainValidationException>(() => LedgerEntry.CreateTransferIn(accountId, Money.Zero, "ref"));
    }

    [Fact]
    public void LedgerEntry_IsEntity_EqualityById()
    {
        var accountId = ValidAccountId;
        var amount = Money.FromDecimal(100m);
        var e1 = LedgerEntry.CreateDeposit(accountId, amount);
        var e2 = LedgerEntry.CreateDeposit(accountId, amount);
        // Different Ids => not equal
        Assert.NotEqual(e1, e2);
        Assert.NotEqual(e1.Id, e2.Id);
    }

    [Fact]
    public void Create_CreatesUniqueIds()
    {
        var accountId = ValidAccountId;
        var amount = Money.FromDecimal(10m);
        var e1 = LedgerEntry.Create(accountId, amount, LedgerEntryType.Deposit);
        var e2 = LedgerEntry.Create(accountId, amount, LedgerEntryType.Deposit);
        Assert.NotEqual(e1.Id, e2.Id);
    }
}

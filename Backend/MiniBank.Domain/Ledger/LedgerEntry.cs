using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks.ValueObjects;

namespace MiniBank.Domain.Ledger;

/// <summary>Immutable, append-only ledger entry.</summary>
public sealed class LedgerEntry : Entity<Guid>
{
    public AccountId AccountId { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;
    public LedgerEntryType Type { get; private set; }
    public DateTimeOffset OccurredOn { get; private set; }
    public string? ReferenceId { get; private set; }
    public string? Description { get; private set; }

    private LedgerEntry() { }

    private LedgerEntry(Guid id, AccountId accountId, Money amount, LedgerEntryType type, string? referenceId, string? description, DateTimeOffset occurredOn)
        : base(id)
    {
        AccountId = accountId;
        Amount = amount;
        Type = type;
        OccurredOn = occurredOn;
        ReferenceId = referenceId;
        Description = description;
    }

    private LedgerEntry(Guid id, AccountId accountId, Money amount, LedgerEntryType type, string? referenceId, string? description)
        : this(id, accountId, amount, type, referenceId, description, DateTimeOffset.UtcNow)
    {
    }

    public static LedgerEntry Create(AccountId accountId, Money amount, LedgerEntryType type, string? referenceId = null, string? description = null)
    {
        if (amount.IsZero)
            throw new BuildingBlocks.Exceptions.DomainValidationException(nameof(amount), "Amount must be positive for ledger entry.");

        return new LedgerEntry(Guid.NewGuid(), accountId, amount, type, referenceId, description);
    }

    internal static LedgerEntry CreateWithTimestamp(AccountId accountId, Money amount, LedgerEntryType type, DateTimeOffset occurredOn, string? referenceId = null, string? description = null)
    {
        if (amount.IsZero)
            throw new BuildingBlocks.Exceptions.DomainValidationException(nameof(amount), "Amount must be positive for ledger entry.");

        return new LedgerEntry(Guid.NewGuid(), accountId, amount, type, referenceId, description, occurredOn);
    }

    public static LedgerEntry CreateDeposit(AccountId accountId, Money amount, string? referenceId = null)
        => Create(accountId, amount, LedgerEntryType.Deposit, referenceId, "Deposit");

    public static LedgerEntry CreateWithdraw(AccountId accountId, Money amount, string? referenceId = null)
        => Create(accountId, amount, LedgerEntryType.Withdraw, referenceId, "Withdraw");

    public static LedgerEntry CreateTransferOut(AccountId accountId, Money amount, string referenceId)
        => Create(accountId, amount, LedgerEntryType.TransferOut, referenceId, "Transfer Out");

    public static LedgerEntry CreateTransferIn(AccountId accountId, Money amount, string referenceId)
        => Create(accountId, amount, LedgerEntryType.TransferIn, referenceId, "Transfer In");
}

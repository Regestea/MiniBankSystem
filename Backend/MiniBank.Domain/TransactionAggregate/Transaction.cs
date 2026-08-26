using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.Ledger;
using MiniBank.Domain.TransactionAggregate.Events;
using MiniBank.Domain.TransactionAggregate.ValueObjects;

namespace MiniBank.Domain.TransactionAggregate;

/// <summary>
/// Transaction is the banking source of truth — immutable, append-only.
/// Every money movement is a Transaction. For Transfer, one Transaction creates two postings (double-entry) with same ReferenceId.
/// Balance is always calculated from ordered Transactions/Postings, not from cached field.
/// Like real core banking: Journal -> Ledger.
/// </summary>
public sealed class Transaction : AggregateRoot<TransactionId>
{
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; } = null!;

    // For Deposit/Withdraw: only one side is set
    // For Transfer: both are set
    public AccountId? SourceAccountId { get; private set; }
    public AccountId? DestinationAccountId { get; private set; }

    // Convenience for single-account transactions
    public AccountId? AccountId => Type switch
    {
        TransactionType.Deposit => DestinationAccountId,
        TransactionType.Withdraw => SourceAccountId,
        TransactionType.Transfer => null, // use Source/Destination
        _ => null
    };

    public DateTimeOffset OccurredOn { get; private set; }
    public string ReferenceId { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private readonly List<LedgerEntry> _postings = new();

    /// <summary>
    /// Double-entry postings of this Transaction — 1 entry for Deposit/Withdraw, 2 for Transfer.
    /// All postings share the same OccurredOn and ReferenceId as the Transaction.
    /// </summary>
    public IReadOnlyList<LedgerEntry> Postings => _postings.AsReadOnly();

    private Transaction() { }

    private Transaction(TransactionId id, TransactionType type, Money amount, AccountId? source, AccountId? destination, string? description = null, string? referenceId = null)
        : base(id)
    {
        Type = type;
        Amount = amount;
        SourceAccountId = source;
        DestinationAccountId = destination;
        OccurredOn = DateTimeOffset.UtcNow;
        ReferenceId = NormalizeReference(referenceId) ?? id.Value.ToString("N");
        Description = description;
        CreatedAt = OccurredOn;
        UpdatedAt = OccurredOn;

        // Create postings once — double-entry, same OccurredOn, same ReferenceId, stable Ids
        _postings.AddRange(CreatePostingsInternal());

        AddDomainEvent(new TransactionCreatedEvent(id, type.ToString(), amount, source?.ToString(), destination?.ToString()));

        // Also raise specific money event for compatibility — uses actual posting Ids
        if (type == TransactionType.Transfer)
        {
            AddDomainEvent(new MoneyTransferredEvent(source!, destination!, amount, ReferenceId, _postings[0].Id, _postings[1].Id));
        }
    }

    private static string? NormalizeReference(string? referenceId)
        => string.IsNullOrWhiteSpace(referenceId) ? null : referenceId.Trim();

    private IReadOnlyList<LedgerEntry> CreatePostingsInternal()
    {
        return Type switch
        {
            TransactionType.Deposit => new List<LedgerEntry>
            {
                LedgerEntry.CreateWithTimestamp(DestinationAccountId!, Amount, LedgerEntryType.Deposit, OccurredOn, ReferenceId, "Deposit")
            },
            TransactionType.Withdraw => new List<LedgerEntry>
            {
                LedgerEntry.CreateWithTimestamp(SourceAccountId!, Amount, LedgerEntryType.Withdraw, OccurredOn, ReferenceId, "Withdraw")
            },
            TransactionType.Transfer => new List<LedgerEntry>
            {
                LedgerEntry.CreateWithTimestamp(SourceAccountId!, Amount, LedgerEntryType.TransferOut, OccurredOn, ReferenceId, "Transfer Out"),
                LedgerEntry.CreateWithTimestamp(DestinationAccountId!, Amount, LedgerEntryType.TransferIn, OccurredOn, ReferenceId, "Transfer In")
            },
            _ => throw new DomainInvariantViolationException(nameof(Type), $"Unknown transaction type {Type}")
        };
    }

    // ========== Factories — all validation is here, like real banking ==========

    public static Transaction CreateDeposit(AccountId accountId, Money amount, string? description = null, string? referenceId = null)
    {
        if (accountId is null)
            throw new DomainValidationException(nameof(accountId), "AccountId cannot be null.");
        if (amount is null)
            throw new DomainValidationException(nameof(amount), "Amount cannot be null.");
        if (amount.IsZero)
            throw new DomainValidationException(nameof(amount), "Deposit amount must be greater than zero.");

        var id = new TransactionId(Guid.NewGuid());
        return new Transaction(id, TransactionType.Deposit, amount, null, accountId, description ?? "Deposit", referenceId);
    }

    public static Transaction CreateWithdraw(AccountId accountId, Money amount, Money currentOrderedBalance, string? description = null, string? referenceId = null)
    {
        if (accountId is null)
            throw new DomainValidationException(nameof(accountId), "AccountId cannot be null.");
        if (amount is null || currentOrderedBalance is null)
            throw new DomainValidationException(nameof(amount), "Amount and balance cannot be null.");
        if (amount.IsZero)
            throw new DomainValidationException(nameof(amount), "Withdraw amount must be greater than zero.");
        if (currentOrderedBalance < amount)
            throw new DomainInvariantViolationException(nameof(amount), $"Insufficient funds. Balance (ordered): {currentOrderedBalance.Amount}, Requested: {amount.Amount}");

        var id = new TransactionId(Guid.NewGuid());
        return new Transaction(id, TransactionType.Withdraw, amount, accountId, null, description ?? "Withdraw", referenceId);
    }

    public static Transaction CreateTransfer(AccountId fromAccountId, AccountId toAccountId, Money amount, Money fromOrderedBalance, string? description = null, string? referenceId = null)
    {
        if (fromAccountId is null)
            throw new DomainValidationException(nameof(fromAccountId), "Source account cannot be null.");
        if (toAccountId is null)
            throw new DomainValidationException(nameof(toAccountId), "Destination account cannot be null.");
        if (fromAccountId.Equals(toAccountId))
            throw new DomainValidationException(nameof(toAccountId), "Cannot transfer to the same account.");
        if (amount is null || fromOrderedBalance is null)
            throw new DomainValidationException(nameof(amount), "Amount and balance cannot be null.");
        if (amount.IsZero)
            throw new DomainValidationException(nameof(amount), "Transfer amount must be greater than zero.");
        if (fromOrderedBalance < amount)
            throw new DomainInvariantViolationException(nameof(amount), $"Insufficient funds for transfer. Balance (ordered): {fromOrderedBalance.Amount}, Requested: {amount.Amount}");

        var id = new TransactionId(Guid.NewGuid());
        return new Transaction(id, TransactionType.Transfer, amount, fromAccountId, toAccountId, description ?? "Transfer", referenceId);
    }

    /// <summary>
    /// Double-entry postings of this Transaction — 1 for Deposit/Withdraw, 2 for Transfer. Cached, stable Ids.
    /// </summary>
    public IReadOnlyList<LedgerEntry> ToLedgerEntries() => _postings.AsReadOnly();

    /// <summary>
    /// For Transfer, returns the two entries separately for applying to two accounts.
    /// </summary>
    public (LedgerEntry FromEntry, LedgerEntry ToEntry) ToTransferEntries()
    {
        if (Type != TransactionType.Transfer)
            throw new DomainOperationNotAllowedException(nameof(Type), "Only Transfer transaction has two entries");
        return (_postings[0], _postings[1]);
    }

    // For rehydration — postings must be passed to preserve original posting Ids after persistence/replay
    private Transaction(TransactionId id, TransactionType type, Money amount, AccountId? source, AccountId? destination, DateTimeOffset occurredOn, string referenceId, IEnumerable<LedgerEntry>? postings)
        : base(id)
    {
        Type = type;
        Amount = amount;
        SourceAccountId = source;
        DestinationAccountId = destination;
        OccurredOn = occurredOn;
        ReferenceId = referenceId;
        CreatedAt = occurredOn;
        UpdatedAt = occurredOn;

        // Preserve stored posting Ids when rehydrating; recreate only when not provided (tests/in-memory)
        if (postings is null)
            _postings.AddRange(CreatePostingsInternal());
        else
            _postings.AddRange(postings);
    }

    /// <summary>
    /// Rehydrates a persisted Transaction. Pass the stored <paramref name="postings"/> so original
    /// LedgerEntry Ids survive replay (critical for MoneyTransferredEvent correlation).
    /// </summary>
    public static Transaction Rehydrate(
        TransactionId id,
        TransactionType type,
        Money amount,
        AccountId? source,
        AccountId? destination,
        DateTimeOffset occurredOn,
        string referenceId,
        int version,
        IEnumerable<LedgerEntry>? postings = null)
    {
        var tx = new Transaction(id, type, amount, source, destination, occurredOn, referenceId, postings);
        tx.Version = version;
        return tx;
    }
}

using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BeneficiaryAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Domain.BeneficiaryAggregate;

/// <summary>
/// Saved transfer destination (beneficiary).
/// Owner ticks "save" during a transfer so the account number + holder name
/// is remembered for next transfers. Scoped per owner: (owner, account_number) is unique.
/// Amounts are always USD (see Money) — no currency stored here.
/// </summary>
public sealed class Beneficiary : AggregateRoot<BeneficiaryId>
{
    public CustomerId OwnerCustomerId { get; private set; } = null!;
    public AccountNumber AccountNumber { get; private set; } = null!;
    public string HolderName { get; private set; } = string.Empty;

    private Beneficiary() { }

    private Beneficiary(BeneficiaryId id, CustomerId ownerCustomerId, AccountNumber accountNumber, string holderName)
        : base(id)
    {
        OwnerCustomerId = ownerCustomerId;
        AccountNumber = accountNumber;
        HolderName = holderName;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Beneficiary Create(CustomerId ownerCustomerId, AccountNumber accountNumber, string holderName, BeneficiaryId? id = null)
    {
        if (ownerCustomerId is null)
            throw new DomainValidationException(nameof(ownerCustomerId), "OwnerCustomerId cannot be null.");
        if (accountNumber is null)
            throw new DomainValidationException(nameof(accountNumber), "AccountNumber cannot be null.");
        if (string.IsNullOrWhiteSpace(holderName))
            throw new DomainValidationException(nameof(holderName), "Holder name cannot be empty.");

        holderName = holderName.Trim();
        if (holderName.Length is < 2 or > 100)
            throw new DomainValidationException(nameof(holderName), "Holder name must be 2-100 characters.");

        id ??= new BeneficiaryId(Guid.NewGuid());
        return new Beneficiary(id, ownerCustomerId, accountNumber, holderName);
    }

    public void Rename(string holderName)
    {
        if (string.IsNullOrWhiteSpace(holderName))
            throw new DomainValidationException(nameof(holderName), "Holder name cannot be empty.");
        holderName = holderName.Trim();
        if (holderName.Length is < 2 or > 100)
            throw new DomainValidationException(nameof(holderName), "Holder name must be 2-100 characters.");

        HolderName = holderName;
        IncrementVersion();
    }

    private Beneficiary(BeneficiaryId id, CustomerId ownerCustomerId, AccountNumber accountNumber, string holderName, int version, DateTimeOffset createdAt, DateTimeOffset updatedAt)
        : base(id)
    {
        OwnerCustomerId = ownerCustomerId;
        AccountNumber = accountNumber;
        HolderName = holderName;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Beneficiary Rehydrate(BeneficiaryId id, CustomerId ownerCustomerId, AccountNumber accountNumber, string holderName, int version, DateTimeOffset createdAt, DateTimeOffset updatedAt)
        => new(id, ownerCustomerId, accountNumber, holderName, version, createdAt, updatedAt);
}

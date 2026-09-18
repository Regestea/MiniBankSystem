using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.BeneficiaryAggregate.ValueObjects;

public sealed record BeneficiaryId
{
    public Guid Value { get; }

    public BeneficiaryId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainValidationException(nameof(BeneficiaryId), "BeneficiaryId cannot be empty.");
        Value = value;
    }

    public static implicit operator Guid(BeneficiaryId id) => id.Value;
    public static implicit operator BeneficiaryId(Guid id) => new(id);

    public override string ToString() => Value.ToString();
}

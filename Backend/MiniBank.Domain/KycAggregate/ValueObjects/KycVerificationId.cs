using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.KycAggregate.ValueObjects;

public sealed record KycVerificationId
{
    public Guid Value { get; }

    public KycVerificationId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainValidationException(nameof(KycVerificationId), "KycVerificationId cannot be empty.");

        Value = value;
    }

    public static implicit operator Guid(KycVerificationId id) => id.Value;
    public static implicit operator KycVerificationId(Guid id) => new(id);

    public override string ToString() => Value.ToString();
}

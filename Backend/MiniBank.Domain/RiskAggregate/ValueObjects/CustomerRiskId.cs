using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.RiskAggregate.ValueObjects;

public sealed record CustomerRiskId
{
    public Guid Value { get; }

    public CustomerRiskId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainValidationException(nameof(CustomerRiskId), "CustomerRiskId cannot be empty.");

        Value = value;
    }

    public static implicit operator Guid(CustomerRiskId id) => id.Value;
    public static implicit operator CustomerRiskId(Guid id) => new(id);

    public override string ToString() => Value.ToString();
}

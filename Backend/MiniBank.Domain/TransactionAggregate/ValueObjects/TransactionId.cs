using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.TransactionAggregate.ValueObjects;

public sealed record TransactionId
{
    public Guid Value { get; }

    public TransactionId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainValidationException(nameof(TransactionId), "TransactionId cannot be empty.");
        Value = value;
    }

    public static implicit operator Guid(TransactionId id) => id.Value;
    public static implicit operator TransactionId(Guid id) => new(id);

    public override string ToString() => Value.ToString();
}

using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.BuildingBlocks.ValueObjects;

public sealed record OutboxMessageId
{
    public Guid Value { get; }

    public OutboxMessageId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainValidationException(nameof(OutboxMessageId), "OutboxMessageId cannot be empty.");

        Value = value;
    }

    public static implicit operator Guid(OutboxMessageId id) => id.Value;
    public static implicit operator OutboxMessageId(Guid id) => new(id);

    public override string ToString() => Value.ToString();
}
using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.DocumentAggregate.ValueObjects;

public sealed record DocumentId
{
    public Guid Value { get; }

    public DocumentId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainValidationException(nameof(DocumentId), "DocumentId cannot be empty.");

        Value = value;
    }

    public static implicit operator Guid(DocumentId id) => id.Value;
    public static implicit operator DocumentId(Guid id) => new(id);

    public override string ToString() => Value.ToString();
}

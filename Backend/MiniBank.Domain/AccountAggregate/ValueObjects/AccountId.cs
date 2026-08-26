using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.AccountAggregate.ValueObjects;

public record AccountId
{
    public Guid Value { get; }

    public AccountId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainValidationException(nameof(AccountId), "AccountId cannot be empty.");
        Value = value;
    }

    public static implicit operator Guid(AccountId id) => id.Value;
    public static implicit operator AccountId(Guid id) => new(id);

    public override string ToString() => Value.ToString();
}

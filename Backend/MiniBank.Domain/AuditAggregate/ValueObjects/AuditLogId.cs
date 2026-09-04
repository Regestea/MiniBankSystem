using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.AuditAggregate.ValueObjects;

public sealed record AuditLogId
{
    public Guid Value { get; }

    public AuditLogId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainValidationException(nameof(AuditLogId), "AuditLogId cannot be empty.");

        Value = value;
    }

    public static implicit operator Guid(AuditLogId id) => id.Value;
    public static implicit operator AuditLogId(Guid id) => new(id);

    public override string ToString() => Value.ToString();
}

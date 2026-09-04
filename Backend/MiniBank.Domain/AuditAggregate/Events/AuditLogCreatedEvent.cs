using MiniBank.Domain.BuildingBlocks;

namespace MiniBank.Domain.AuditAggregate.Events;

public sealed record AuditLogCreatedEvent(
    Guid AuditLogId,
    Guid UserId,
    string EntityType,
    string EntityId
) : DomainEvent;

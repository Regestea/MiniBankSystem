using System.Text.Json;
using MiniBank.Domain.AuditAggregate.Events;
using MiniBank.Domain.AuditAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;

namespace MiniBank.Domain.AuditAggregate;

public sealed class AuditLog : AggregateRoot<AuditLogId>
{
    public Guid UserId { get; private set; }
    public string UserEmail { get; private set; } = null!;
    public AuditAction Action { get; private set; }
    public string EntityType { get; private set; } = null!;
    public string EntityId { get; private set; } = null!;
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? Description { get; private set; }
    public string? IpAddress { get; private set; }

    private AuditLog() { }

    private AuditLog(
        AuditLogId id,
        Guid userId,
        string userEmail,
        AuditAction action,
        string entityType,
        string entityId,
        string? oldValues,
        string? newValues,
        string? description,
        string? ipAddress)
        : base(id)
    {
        UserId = userId;
        UserEmail = userEmail;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        OldValues = oldValues;
        NewValues = newValues;
        Description = description;
        IpAddress = ipAddress;
        CreatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new AuditLogCreatedEvent(id, userId, entityType, entityId));
    }

    public static AuditLog Create(
        Guid userId,
        string userEmail,
        AuditAction action,
        string entityType,
        string entityId,
        string? oldValues,
        string? newValues,
        string? description,
        string? ipAddress,
        AuditLogId? id = null)
    {
        id ??= new AuditLogId(Guid.NewGuid());
        return new AuditLog(id, userId, userEmail, action, entityType, entityId,
                           oldValues, newValues, description, ipAddress);
    }

    private AuditLog(
        AuditLogId id,
        Guid userId,
        string userEmail,
        AuditAction action,
        string entityType,
        string entityId,
        string? oldValues,
        string? newValues,
        string? description,
        string? ipAddress,
        DateTimeOffset createdAt)
        : base(id)
    {
        UserId = userId;
        UserEmail = userEmail;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        OldValues = oldValues;
        NewValues = newValues;
        Description = description;
        IpAddress = ipAddress;
        CreatedAt = createdAt;
    }

    public static AuditLog Rehydrate(
        AuditLogId id,
        Guid userId,
        string userEmail,
        AuditAction action,
        string entityType,
        string entityId,
        string? oldValues,
        string? newValues,
        string? description,
        string? ipAddress,
        DateTimeOffset createdAt)
        => new(id, userId, userEmail, action, entityType, entityId,
               oldValues, newValues, description, ipAddress, createdAt);
}

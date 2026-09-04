using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniBank.Domain.AuditAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Abstractions;

namespace MiniBank.Infrastructure.Persistence;

/// <summary>
/// DI wrapper for <see cref="MiniBankDbContext"/> as <see cref="IUnitOfWork"/>.
/// Audit logs are created inline (same transaction) before SaveChanges.
/// Domain events are persisted to outbox table in the same transaction for reliability.
/// A separate background processor reads the outbox and dispatches to handlers.
/// </summary>
internal sealed class EfUnitOfWork(
    MiniBankDbContext db,
    IServiceProvider serviceProvider,
    ICurrentUserContext currentUser,
    IHttpContextAccessor httpContextAccessor,
    ILogger<EfUnitOfWork> logger) : IUnitOfWork
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IDomainEvent, CancellationToken, Task>> Dispatchers = new();

    private (Guid UserId, string Email) GetCurrentUserSafe()
    {
        try
        {
            return (currentUser.UserId, currentUser.Email ?? "anonymous");
        }
        catch (UnauthorizedAccessException)
        {
            return (Guid.Empty, "anonymous");
        }
    }

    private string? GetClientIpAddress()
    {
        try
        {
            return httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public void DetachAll()
    {
        foreach (var entry in db.ChangeTracker.Entries().ToList())
            entry.State = EntityState.Detached;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = CollectDomainEvents();

        var auditLogs = BuildAuditLogs(domainEvents);
        if (auditLogs.Count > 0)
            await db.AuditLogs.AddRangeAsync(auditLogs, cancellationToken);

        var outboxMessages = BuildOutboxMessages(domainEvents);
        if (outboxMessages.Count > 0)
            await db.OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);

        int result;
        try
        {
            result = await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("concurrency", "The resource was modified by another operation. Please retry.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.NpgsqlException pgEx && pgEx.SqlState == "23505")
        {
            throw new UniqueConstraintViolationException("reference", "A resource with the same key already exists.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.NpgsqlException pgEx && pgEx.SqlState == "40P01")
        {
            // Postgres deadlock (e.g. concurrent opposite transfers A->B + B->A locking
            // rows in opposite order). Treat as a retryable concurrency conflict so
            // handlers with MaxRetries can reload and retry instead of returning 500.
            throw new ConcurrencyConflictException("concurrency", "The operation deadlocked with a concurrent transaction. Please retry.", ex);
        }

        return result;
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        var aggregates = db.ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());
        return events;
    }

    private List<AuditLog> BuildAuditLogs(List<IDomainEvent> domainEvents)
    {
        var logs = new List<AuditLog>();
        var (userId, email) = GetCurrentUserSafe();
        var ipAddress = GetClientIpAddress();

        foreach (var domainEvent in domainEvents)
        {
            var log = domainEvent switch
            {
                // Customer events
                Domain.CustomerAggregate.Events.CustomerCreatedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Create, "Customer", e.CustomerId.Value.ToString(),
                        null, JsonSerializer.Serialize(new { FullName = (string)e.FullName, Email = (string)e.Email }),
                        "Customer registered", ipAddress),

                Domain.CustomerAggregate.Events.CustomerVerifiedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Verify, "Customer", e.CustomerId.Value.ToString(),
                        null, JsonSerializer.Serialize(new { Status = "Verified" }),
                        "Customer verified", ipAddress),

                Domain.CustomerAggregate.Events.CustomerBlockedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Block, "Customer", e.CustomerId.Value.ToString(),
                        null, JsonSerializer.Serialize(new { Status = "Blocked" }),
                        "Customer blocked", ipAddress),

                Domain.CustomerAggregate.Events.CustomerUpdatedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Update, "Customer", e.CustomerId.Value.ToString(),
                        null, JsonSerializer.Serialize(new { FullName = (string)e.FullName, PhoneNumber = (string)e.PhoneNumber }),
                        "Customer profile updated", ipAddress),

                // Account events
                Domain.AccountAggregate.Events.AccountOpenedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Create, "Account", e.AccountId.Value.ToString(),
                        null, JsonSerializer.Serialize(new { e.AccountNumber.Value, AccountType = e.AccountType.ToString() }),
                        "Account opened", ipAddress),

                Domain.AccountAggregate.Events.AccountApprovedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Approve, "Account", e.AccountId.Value.ToString(),
                        null, JsonSerializer.Serialize(new { Status = "Active" }),
                        "Account approved", ipAddress),

                Domain.AccountAggregate.Events.AccountRejectedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Reject, "Account", e.AccountId.Value.ToString(),
                        null, JsonSerializer.Serialize(new { Status = "Closed", e.Reason }),
                        "Account rejected", ipAddress),

                Domain.AccountAggregate.Events.AccountFrozenEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Update, "Account", e.AccountId.Value.ToString(),
                        null, JsonSerializer.Serialize(new { Status = "Frozen" }),
                        "Account frozen", ipAddress),

                Domain.AccountAggregate.Events.AccountUnfrozenEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Update, "Account", e.AccountId.Value.ToString(),
                        null, JsonSerializer.Serialize(new { Status = "Active" }),
                        "Account unfrozen", ipAddress),

                Domain.AccountAggregate.Events.AccountClosedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Update, "Account", e.AccountId.Value.ToString(),
                        null, JsonSerializer.Serialize(new { Status = "Closed" }),
                        "Account closed", ipAddress),

                // Transaction events
                Domain.TransactionAggregate.Events.TransactionCreatedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Create, "Transaction", e.TransactionId.Value.ToString(),
                        null, JsonSerializer.Serialize(new { Type = e.TransactionType, Amount = e.Amount.Amount, SourceAccountId = e.SourceAccountId, DestinationAccountId = e.DestinationAccountId }),
                        "Transaction created", ipAddress),

                // Risk events
                Domain.RiskAggregate.Events.RiskLevelChangedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Update, "CustomerRisk", e.CustomerId.ToString(),
                        JsonSerializer.Serialize(new { OldLevel = e.OldLevel.ToString() }),
                        JsonSerializer.Serialize(new { NewLevel = e.NewLevel.ToString() }),
                        "Risk level changed", ipAddress),

                // Document events
                Domain.DocumentAggregate.Events.DocumentUploadedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Create, "Document", e.DocumentId.ToString(),
                        null, JsonSerializer.Serialize(new { e.FileName, Type = e.Type.ToString() }),
                        "Document uploaded", ipAddress),

                Domain.DocumentAggregate.Events.DocumentVerifiedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Verify, "Document", e.DocumentId.ToString(),
                        null, JsonSerializer.Serialize(new { Status = "Verified" }),
                        "Document verified", ipAddress),

                Domain.DocumentAggregate.Events.DocumentRejectedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Reject, "Document", e.DocumentId.ToString(),
                        null, JsonSerializer.Serialize(new { Status = "Rejected", e.Reason }),
                        "Document rejected", ipAddress),

                // KYC events
                Domain.KycAggregate.Events.KycSubmittedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Create, "KycVerification", e.KycId.ToString(),
                        null, JsonSerializer.Serialize(new { CustomerId = e.CustomerId }),
                        "KYC submitted", ipAddress),

                Domain.KycAggregate.Events.KycApprovedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Approve, "KycVerification", e.KycId.ToString(),
                        null, JsonSerializer.Serialize(new { Status = "Approved" }),
                        "KYC approved", ipAddress),

                Domain.KycAggregate.Events.KycRejectedEvent e
                    => AuditLog.Create(userId, email,
                        AuditAction.Reject, "KycVerification", e.KycId.ToString(),
                        null, JsonSerializer.Serialize(new { Status = "Rejected", e.Reason }),
                        "KYC rejected", ipAddress),

                // Money lifecycle events (Deposit/Withdraw/Transfer) are intentionally NOT audited:
                // TransactionCreatedEvent above is the audit record. These remain as integration
                // events for future subscribers (notifications/read models) via outbox.
                Domain.TransactionAggregate.Events.MoneyDepositedEvent
                    or Domain.TransactionAggregate.Events.MoneyWithdrawnEvent
                    or Domain.TransactionAggregate.Events.MoneyTransferredEvent
                    => null,

                _ => null
            };

            if (log is not null)
                logs.Add(log);
        }

        return logs;
    }

    private List<OutboxMessage> BuildOutboxMessages(List<IDomainEvent> domainEvents)
    {
        var messages = new List<OutboxMessage>();
        var occurredOn = DateTimeOffset.UtcNow;

        foreach (var domainEvent in domainEvents)
        {
            // Skip audit-only events (they're already persisted in audit_logs)
            var isAuditOnly = domainEvent switch
            {
                Domain.CustomerAggregate.Events.CustomerCreatedEvent => true,
                Domain.CustomerAggregate.Events.CustomerVerifiedEvent => true,
                Domain.CustomerAggregate.Events.CustomerBlockedEvent => true,
                Domain.CustomerAggregate.Events.CustomerUpdatedEvent => true,
                Domain.AccountAggregate.Events.AccountOpenedEvent => true,
                Domain.AccountAggregate.Events.AccountApprovedEvent => true,
                Domain.AccountAggregate.Events.AccountRejectedEvent => true,
                Domain.AccountAggregate.Events.AccountFrozenEvent => true,
                Domain.AccountAggregate.Events.AccountUnfrozenEvent => true,
                Domain.AccountAggregate.Events.AccountClosedEvent => true,
                Domain.TransactionAggregate.Events.TransactionCreatedEvent => true,
                Domain.RiskAggregate.Events.RiskLevelChangedEvent => true,
                Domain.DocumentAggregate.Events.DocumentUploadedEvent => true,
                Domain.DocumentAggregate.Events.DocumentVerifiedEvent => true,
                Domain.DocumentAggregate.Events.DocumentRejectedEvent => true,
                Domain.KycAggregate.Events.KycSubmittedEvent => true,
                Domain.KycAggregate.Events.KycApprovedEvent => true,
                Domain.KycAggregate.Events.KycRejectedEvent => true,
                _ => false
            };

            if (isAuditOnly)
                continue;

            var payload = JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var message = OutboxMessage.Create(
                domainEvent.GetType().Name,
                payload,
                occurredOn);

            messages.Add(message);
        }

        return messages;
    }
}
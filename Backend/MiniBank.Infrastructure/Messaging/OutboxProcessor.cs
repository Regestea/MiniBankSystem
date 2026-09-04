using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Infrastructure.Persistence;

namespace MiniBank.Infrastructure.Messaging;

/// <summary>
/// Background service that polls the outbox table and dispatches events to registered handlers.
/// Runs in the same process as the API (no separate worker needed for sample).
/// </summary>
internal sealed class OutboxProcessor(
    IServiceProvider serviceProvider,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private static readonly ConcurrentDictionary<string, Type> EventTypeCache = new();
    private const int BatchSize = 100;
    private const int MaxRetries = 5;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox batch");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Outbox processor stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MiniBankDbContext>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedOn == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredOn)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await DispatchAsync(message, scope.ServiceProvider, cancellationToken);
                message.MarkProcessed();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process outbox message {MessageId} (type: {EventType}, retry: {RetryCount})",
                    message.Id.Value, message.EventType, message.RetryCount + 1);
                message.MarkFailed(ex.Message);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchAsync(OutboxMessage message, IServiceProvider sp, CancellationToken cancellationToken)
    {
        var eventType = GetEventType(message.EventType);
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handleMethod = handlerType.GetMethod("HandleAsync")
            ?? throw new InvalidOperationException($"HandleAsync not found on '{handlerType.Name}'.");

        var handlers = sp.GetServices(handlerType).ToList();
        if (handlers.Count == 0)
        {
            logger.LogDebug("No handlers registered for event type {EventType}", message.EventType);
            return;
        }

        var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) as IDomainEvent;

        if (domainEvent is null)
        {
            logger.LogWarning("Failed to deserialize outbox message {MessageId} as {EventType}", message.Id.Value, message.EventType);
            return;
        }

        var tasks = handlers.Select(h => (Task)handleMethod.Invoke(h, [domainEvent, cancellationToken])!);
        await Task.WhenAll(tasks);
    }

    private static Type GetEventType(string eventTypeName)
    {
        return EventTypeCache.GetOrAdd(eventTypeName, name =>
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName?.StartsWith("MiniBank.") == true);

            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType($"MiniBank.Domain.{name}");
                if (type is not null)
                    return type;

                // Check in Events sub-namespaces
                type = assembly.GetType($"MiniBank.Domain.CustomerAggregate.Events.{name}");
                if (type is not null) return type;

                type = assembly.GetType($"MiniBank.Domain.AccountAggregate.Events.{name}");
                if (type is not null) return type;

                type = assembly.GetType($"MiniBank.Domain.TransactionAggregate.Events.{name}");
                if (type is not null) return type;

                type = assembly.GetType($"MiniBank.Domain.RiskAggregate.Events.{name}");
                if (type is not null) return type;

                type = assembly.GetType($"MiniBank.Domain.DocumentAggregate.Events.{name}");
                if (type is not null) return type;

                type = assembly.GetType($"MiniBank.Domain.KycAggregate.Events.{name}");
                if (type is not null) return type;

                type = assembly.GetType($"MiniBank.Domain.AuditAggregate.Events.{name}");
                if (type is not null) return type;
            }

            throw new InvalidOperationException($"Event type '{name}' not found in MiniBank.Domain assemblies");
        });
    }
}
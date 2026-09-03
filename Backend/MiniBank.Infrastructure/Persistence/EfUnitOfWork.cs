using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniBank.Domain.BuildingBlocks;

namespace MiniBank.Infrastructure.Persistence;

/// <summary>
/// DI wrapper for <see cref="MiniBankDbContext"/> as <see cref="IUnitOfWork"/>.
/// After a successful save, collected domain events are dispatched to registered
/// <see cref="IDomainEventHandler{TEvent}"/> implementations and cleared from the aggregates.
/// </summary>
internal sealed class EfUnitOfWork(MiniBankDbContext db, IServiceProvider serviceProvider) : IUnitOfWork
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IDomainEvent, CancellationToken, Task>> Dispatchers = new();

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await db.SaveChangesAsync(cancellationToken);

        await DispatchDomainEventsAsync(cancellationToken);

        return result;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var aggregates = db.ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (aggregates.Count == 0)
            return;

        // Snapshot and clear first so handlers that save again don't re-dispatch the same events
        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());

        foreach (var domainEvent in events)
            await DispatchAsync(domainEvent, cancellationToken);
    }

    private Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var dispatcher = Dispatchers.GetOrAdd(domainEvent.GetType(), eventType =>
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handleMethod = handlerType.GetMethod("HandleAsync")
                ?? throw new InvalidOperationException($"HandleAsync not found on '{handlerType.Name}'.");

            return (IServiceProvider sp, IDomainEvent evt, CancellationToken ct) =>
                Task.WhenAll(sp.GetServices(handlerType)
                    .Select(h => (Task)handleMethod.Invoke(h, [evt, ct])!));
        });

        return dispatcher(serviceProvider, domainEvent, cancellationToken);
    }
}

namespace MiniBank.Domain.BuildingBlocks;

/// <summary>
/// Handler for a domain event, invoked by the persistence layer after SaveChangesAsync.
/// Implementations live in outer layers (Features/Infrastructure) and are resolved via DI.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

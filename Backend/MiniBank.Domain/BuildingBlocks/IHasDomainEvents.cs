namespace MiniBank.Domain.BuildingBlocks;

/// <summary>
/// Non-generic view of an aggregate that raises domain events.
/// Lets the persistence layer collect and dispatch events without knowing the aggregate's key type.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

namespace MiniBank.Domain.BuildingBlocks;

public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    public int Version { get; protected set; }

    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }

    protected AggregateRoot(TId id) : base(id)
    {
        Version = 0;
    }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void IncrementVersion()
    {
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

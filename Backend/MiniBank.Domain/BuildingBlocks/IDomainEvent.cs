namespace MiniBank.Domain.BuildingBlocks;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
}

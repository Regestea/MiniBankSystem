using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Domain.CustomerAggregate.Events;

public sealed record CustomerBlockedEvent(CustomerId CustomerId) : DomainEvent;

using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Domain.CustomerAggregate.Events;

public sealed record CustomerCreatedEvent(
    CustomerId CustomerId,
    FullName FullName,
    Email Email,
    PhoneNumber PhoneNumber
) : DomainEvent;

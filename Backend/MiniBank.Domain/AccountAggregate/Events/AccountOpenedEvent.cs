using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Domain.AccountAggregate.Events;

public sealed record AccountOpenedEvent(
    AccountId AccountId,
    AccountNumber AccountNumber,
    CustomerId CustomerId,
    AccountType AccountType
) : DomainEvent;

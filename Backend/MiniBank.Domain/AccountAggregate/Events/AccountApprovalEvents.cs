using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;

namespace MiniBank.Domain.AccountAggregate.Events;

public sealed record AccountApprovedEvent(
    AccountId AccountId
) : DomainEvent;

public sealed record AccountRejectedEvent(
    AccountId AccountId,
    string Reason
) : DomainEvent;

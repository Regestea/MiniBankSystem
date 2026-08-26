using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;

namespace MiniBank.Domain.AccountAggregate.Events;

public sealed record AccountFrozenEvent(AccountId AccountId) : DomainEvent;

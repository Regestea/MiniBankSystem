using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.ValueObjects;

namespace MiniBank.Domain.TransactionAggregate.Events;

public sealed record MoneyDepositedEvent(
    AccountId AccountId,
    Money Amount,
    Guid LedgerEntryId
) : DomainEvent;

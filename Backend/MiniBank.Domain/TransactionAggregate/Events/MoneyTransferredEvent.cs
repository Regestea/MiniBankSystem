using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.ValueObjects;

namespace MiniBank.Domain.TransactionAggregate.Events;

public sealed record MoneyTransferredEvent(
    AccountId FromAccountId,
    AccountId ToAccountId,
    Money Amount,
    string TransferId,
    Guid FromLedgerEntryId,
    Guid ToLedgerEntryId
) : DomainEvent;

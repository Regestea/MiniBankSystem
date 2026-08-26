using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.TransactionAggregate.ValueObjects;

namespace MiniBank.Domain.TransactionAggregate.Events;

public sealed record TransactionCreatedEvent(
    TransactionId TransactionId,
    string TransactionType,
    Money Amount,
    string? SourceAccountId,
    string? DestinationAccountId
) : DomainEvent;

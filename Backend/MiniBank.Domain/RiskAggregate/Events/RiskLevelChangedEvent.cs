using MiniBank.Domain.BuildingBlocks;

namespace MiniBank.Domain.RiskAggregate.Events;

public sealed record RiskLevelChangedEvent(
    Guid CustomerRiskId,
    Guid CustomerId,
    RiskLevel OldLevel,
    RiskLevel NewLevel
) : DomainEvent;

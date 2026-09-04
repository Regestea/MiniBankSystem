using MiniBank.Domain.BuildingBlocks;

namespace MiniBank.Domain.KycAggregate.Events;

public sealed record KycSubmittedEvent(
    Guid KycId,
    Guid CustomerId,
    Guid PrimaryDocumentId
) : DomainEvent;

public sealed record KycApprovedEvent(
    Guid KycId,
    Guid CustomerId,
    Guid ReviewedBy
) : DomainEvent;

public sealed record KycRejectedEvent(
    Guid KycId,
    Guid CustomerId,
    Guid ReviewedBy,
    string Reason
) : DomainEvent;

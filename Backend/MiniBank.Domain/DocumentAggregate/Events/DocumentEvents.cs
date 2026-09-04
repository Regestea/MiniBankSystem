using MiniBank.Domain.BuildingBlocks;

namespace MiniBank.Domain.DocumentAggregate.Events;

public sealed record DocumentUploadedEvent(
    Guid DocumentId,
    Guid CustomerId,
    string FileName,
    DocumentType Type
) : DomainEvent;

public sealed record DocumentVerifiedEvent(
    Guid DocumentId,
    Guid VerifiedBy
) : DomainEvent;

public sealed record DocumentRejectedEvent(
    Guid DocumentId,
    Guid VerifiedBy,
    string Reason
) : DomainEvent;

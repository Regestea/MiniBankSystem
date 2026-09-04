using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.KycAggregate.Events;
using MiniBank.Domain.KycAggregate.ValueObjects;

namespace MiniBank.Domain.KycAggregate;

public sealed class KycVerification : AggregateRoot<KycVerificationId>
{
    public Guid CustomerId { get; private set; }
    public KycStatus Status { get; private set; }
    public Guid? PrimaryDocumentId { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public string? RejectionReason { get; private set; }

    private KycVerification() { }

    private KycVerification(KycVerificationId id, Guid customerId)
        : base(id)
    {
        CustomerId = customerId;
        Status = KycStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static KycVerification Create(Guid customerId, KycVerificationId? id = null)
    {
        if (customerId == Guid.Empty)
            throw new DomainValidationException(nameof(customerId), "CustomerId cannot be empty.");

        id ??= new KycVerificationId(Guid.NewGuid());
        return new KycVerification(id, customerId);
    }

    public void Submit(Guid primaryDocumentId)
    {
        if (Status != KycStatus.Pending)
            throw new DomainOperationNotAllowedException(nameof(Status), "Only pending KYC can be submitted.");

        if (primaryDocumentId == Guid.Empty)
            throw new DomainValidationException(nameof(primaryDocumentId), "PrimaryDocumentId cannot be empty.");

        Status = KycStatus.Submitted;
        PrimaryDocumentId = primaryDocumentId;
        SubmittedAt = DateTimeOffset.UtcNow;
        IncrementVersion();
        AddDomainEvent(new KycSubmittedEvent(Id, CustomerId, primaryDocumentId));
    }

    public void Approve(Guid reviewerId)
    {
        if (Status != KycStatus.Submitted)
            throw new DomainOperationNotAllowedException(nameof(Status), "Only submitted KYC can be approved.");

        Status = KycStatus.Approved;
        ReviewedBy = reviewerId;
        ReviewedAt = DateTimeOffset.UtcNow;
        IncrementVersion();
        AddDomainEvent(new KycApprovedEvent(Id, CustomerId, reviewerId));
    }

    public void Reject(Guid reviewerId, string reason)
    {
        if (Status != KycStatus.Submitted)
            throw new DomainOperationNotAllowedException(nameof(Status), "Only submitted KYC can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainValidationException(nameof(reason), "Rejection reason cannot be empty.");

        Status = KycStatus.Rejected;
        RejectionReason = reason;
        ReviewedBy = reviewerId;
        ReviewedAt = DateTimeOffset.UtcNow;
        IncrementVersion();
        AddDomainEvent(new KycRejectedEvent(Id, CustomerId, reviewerId, reason));
    }

    /// <summary>Re-submits after a rejection with a (possibly new) primary document.</summary>
    public void Resubmit(Guid primaryDocumentId)
    {
        if (Status != KycStatus.Rejected)
            throw new DomainOperationNotAllowedException(nameof(Status), "Only rejected KYC can be resubmitted.");

        if (primaryDocumentId == Guid.Empty)
            throw new DomainValidationException(nameof(primaryDocumentId), "PrimaryDocumentId cannot be empty.");

        Status = KycStatus.Submitted;
        PrimaryDocumentId = primaryDocumentId;
        SubmittedAt = DateTimeOffset.UtcNow;
        RejectionReason = null;
        ReviewedAt = null;
        ReviewedBy = null;
        IncrementVersion();
        AddDomainEvent(new KycSubmittedEvent(Id, CustomerId, primaryDocumentId));
    }

    private KycVerification(
        KycVerificationId id,
        Guid customerId,
        KycStatus status,
        Guid? primaryDocumentId,
        DateTimeOffset? submittedAt,
        DateTimeOffset? reviewedAt,
        Guid? reviewedBy,
        string? rejectionReason,
        int version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id)
    {
        CustomerId = customerId;
        Status = status;
        PrimaryDocumentId = primaryDocumentId;
        SubmittedAt = submittedAt;
        ReviewedAt = reviewedAt;
        ReviewedBy = reviewedBy;
        RejectionReason = rejectionReason;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static KycVerification Rehydrate(
        KycVerificationId id,
        Guid customerId,
        KycStatus status,
        Guid? primaryDocumentId,
        DateTimeOffset? submittedAt,
        DateTimeOffset? reviewedAt,
        Guid? reviewedBy,
        string? rejectionReason,
        int version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => new(id, customerId, status, primaryDocumentId, submittedAt, reviewedAt,
               reviewedBy, rejectionReason, version, createdAt, updatedAt);
}

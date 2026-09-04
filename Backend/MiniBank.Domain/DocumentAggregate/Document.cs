using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.DocumentAggregate.Events;
using MiniBank.Domain.DocumentAggregate.ValueObjects;

namespace MiniBank.Domain.DocumentAggregate;

public sealed class Document : AggregateRoot<DocumentId>
{
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "application/pdf"];
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public Guid CustomerId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long FileSize { get; private set; }
    public string StoragePath { get; private set; } = null!;
    public DocumentType Type { get; private set; }
    public DocumentStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? VerifiedBy { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }

    private Document() { }

    private Document(
        DocumentId id,
        Guid customerId,
        string fileName,
        string contentType,
        long fileSize,
        string storagePath,
        DocumentType type)
        : base(id)
    {
        CustomerId = customerId;
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
        StoragePath = storagePath;
        Type = type;
        Status = DocumentStatus.Uploaded;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new DocumentUploadedEvent(id, customerId, fileName, type));
    }

    public static Document Upload(
        Guid customerId,
        string fileName,
        string contentType,
        long fileSize,
        string storagePath,
        DocumentType type,
        DocumentId? id = null)
    {
        if (customerId == Guid.Empty)
            throw new DomainValidationException(nameof(customerId), "CustomerId cannot be empty.");
        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainValidationException(nameof(fileName), "FileName cannot be empty.");
        if (fileName.Length > 255)
            throw new DomainValidationException(nameof(fileName), "FileName cannot exceed 255 characters.");
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new DomainValidationException(nameof(storagePath), "StoragePath cannot be empty.");
        if (!Enum.IsDefined(type))
            throw new DomainValidationException(nameof(type), $"Unknown document type {(int)type}.");

        if (fileSize <= 0)
            throw new DomainValidationException(nameof(fileSize), "FileSize must be positive.");

        if (fileSize > MaxFileSize)
            throw new DomainValidationException(nameof(fileSize), $"FileSize must be less than {MaxFileSize / 1024 / 1024}MB.");

        if (!AllowedContentTypes.Contains(contentType))
            throw new DomainValidationException(nameof(contentType), $"ContentType '{contentType}' is not allowed. Allowed: {string.Join(", ", AllowedContentTypes)}.");

        id ??= new DocumentId(Guid.NewGuid());
        return new Document(id, customerId, fileName, contentType, fileSize, storagePath, type);
    }

    public void Verify(Guid verifierId)
    {
        if (Status != DocumentStatus.Uploaded)
            throw new DomainOperationNotAllowedException(nameof(Status), "Only uploaded documents can be verified.");

        Status = DocumentStatus.Verified;
        VerifiedBy = verifierId;
        VerifiedAt = DateTimeOffset.UtcNow;
        IncrementVersion();
        AddDomainEvent(new DocumentVerifiedEvent(Id, verifierId));
    }

    public void Reject(Guid verifierId, string reason)
    {
        if (Status != DocumentStatus.Uploaded)
            throw new DomainOperationNotAllowedException(nameof(Status), "Only uploaded documents can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainValidationException(nameof(reason), "Rejection reason cannot be empty.");

        Status = DocumentStatus.Rejected;
        RejectionReason = reason;
        VerifiedBy = verifierId;
        VerifiedAt = DateTimeOffset.UtcNow;
        IncrementVersion();
        AddDomainEvent(new DocumentRejectedEvent(Id, verifierId, reason));
    }

    private Document(
        DocumentId id,
        Guid customerId,
        string fileName,
        string contentType,
        long fileSize,
        string storagePath,
        DocumentType type,
        DocumentStatus status,
        string? rejectionReason,
        Guid? verifiedBy,
        DateTimeOffset? verifiedAt,
        int version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id)
    {
        CustomerId = customerId;
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
        StoragePath = storagePath;
        Type = type;
        Status = status;
        RejectionReason = rejectionReason;
        VerifiedBy = verifiedBy;
        VerifiedAt = verifiedAt;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Document Rehydrate(
        DocumentId id,
        Guid customerId,
        string fileName,
        string contentType,
        long fileSize,
        string storagePath,
        DocumentType type,
        DocumentStatus status,
        string? rejectionReason,
        Guid? verifiedBy,
        DateTimeOffset? verifiedAt,
        int version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => new(id, customerId, fileName, contentType, fileSize, storagePath, type,
               status, rejectionReason, verifiedBy, verifiedAt, version, createdAt, updatedAt);
}

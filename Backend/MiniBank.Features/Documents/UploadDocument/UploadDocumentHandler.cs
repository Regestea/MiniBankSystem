using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.DocumentAggregate;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Documents.UploadDocument;

internal sealed class UploadDocumentHandler(
    IDocumentRepository documents,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser) : ICommandHandler<UploadDocumentCommand, UploadDocumentResponse>
{
    public async Task<UploadDocumentResponse> HandleAsync(UploadDocumentCommand command, CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.UserId;

        // Sanitize: strip any directory components (prevent ../../ traversal if
        // FileName is ever used on disk) and keep only the leaf name.
        var safeFileName = Path.GetFileName(command.FileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
            throw new DomainValidationException(nameof(command.FileName), "FileName is invalid.");

        var extension = Path.GetExtension(safeFileName);
        var storagePath = Path.Combine("uploads", customerId.ToString(), $"{Guid.NewGuid()}{extension}");

        var document = Document.Upload(
            customerId,
            safeFileName,
            command.ContentType,
            command.FileSize,
            storagePath,
            command.Type);

        await documents.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UploadDocumentResponse(document.Id.Value, document.FileName, document.Status.ToString(), document.Version);
    }
}

using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.DocumentAggregate;
using MiniBank.Domain.DocumentAggregate.ValueObjects;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Documents.VerifyDocument;

internal sealed class VerifyDocumentHandler(
    IDocumentRepository documents,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser,
    IAccessGuard accessGuard) : ICommandHandler<VerifyDocumentCommand, VerifyDocumentResponse>
{
    public async Task<VerifyDocumentResponse> HandleAsync(VerifyDocumentCommand command, CancellationToken cancellationToken = default)
    {
        var document = await documents.GetByIdAsync(new DocumentId(command.DocumentId), cancellationToken)
            ?? throw new NotFoundException("document", command.DocumentId);

        await accessGuard.EnsureDocumentOwnershipAsync(command.DocumentId, cancellationToken);

        if (command.Approve)
            document.Verify(currentUser.UserId);
        else
            document.Reject(currentUser.UserId, command.Reason!);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new VerifyDocumentResponse(document.Id.Value, document.Status.ToString(), document.Version);
    }
}

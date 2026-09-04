using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.DocumentAggregate;
using MiniBank.Domain.DocumentAggregate.ValueObjects;
using MiniBank.Domain.KycAggregate;
using MiniBank.Domain.KycAggregate.ValueObjects;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Kyc.ReviewKyc;

internal sealed class ReviewKycHandler(
    IKycRepository kycRepo,
    IDocumentRepository documents,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser,
    IAccessGuard accessGuard) : ICommandHandler<ReviewKycCommand, ReviewKycResponse>
{
    public async Task<ReviewKycResponse> HandleAsync(ReviewKycCommand command, CancellationToken cancellationToken = default)
    {
        var kyc = await kycRepo.GetByIdAsync(new KycVerificationId(command.KycId), cancellationToken)
            ?? throw new NotFoundException("kyc", command.KycId);

        await accessGuard.EnsureKycOwnershipAsync(command.KycId, cancellationToken);

        if (command.Approve)
        {
            // Banking gate: cannot approve KYC backed by an unverified/missing document.
            if (kyc.PrimaryDocumentId is null || kyc.PrimaryDocumentId == Guid.Empty)
                throw new DomainInvariantViolationException("document", "Cannot approve KYC without a primary document.");
            var document = await documents.GetByIdAsync(new DocumentId(kyc.PrimaryDocumentId.Value), cancellationToken)
                ?? throw new DomainInvariantViolationException("document", "Primary document not found. Cannot approve KYC.");
            if (document.Status != DocumentStatus.Verified)
                throw new DomainInvariantViolationException("document",
                    $"Cannot approve KYC: primary document is {document.Status}, must be Verified.");
            kyc.Approve(currentUser.UserId);
        }
        else
            kyc.Reject(currentUser.UserId, command.Reason!);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReviewKycResponse(kyc.Id.Value, kyc.Status.ToString(), kyc.Version);
    }
}

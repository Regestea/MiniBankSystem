using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.DocumentAggregate;
using MiniBank.Domain.DocumentAggregate.ValueObjects;
using MiniBank.Domain.KycAggregate;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Kyc.SubmitKyc;

internal sealed class SubmitKycHandler(
    IKycRepository kycRepo,
    IDocumentRepository documents,
    IAccessGuard accessGuard,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser) : ICommandHandler<SubmitKycCommand, SubmitKycResponse>
{
    public async Task<SubmitKycResponse> HandleAsync(SubmitKycCommand command, CancellationToken cancellationToken = default)
    {
        // Force CustomerId to be the current user — ignore client-provided value
        var customerId = currentUser.UserId;

        var existing = await kycRepo.GetByCustomerIdAsync(customerId, cancellationToken);
        // IDOR fix: PrimaryDocumentId must exist and belong to the caller.
        // Without this, an attacker could link a victim's document to their own KYC.
        var document = await documents.GetByIdAsync(new DocumentId(command.PrimaryDocumentId), cancellationToken)
            ?? throw new NotFoundException("document", command.PrimaryDocumentId);
        await accessGuard.EnsureDocumentOwnershipAsync(command.PrimaryDocumentId, cancellationToken);

        if (existing is not null)
        {
            // Rejected KYC can be resubmitted (same row, keeps ux_kyc_customer intact).
            // Any other status is a conflict.
            if (existing.Status != KycStatus.Rejected)
                throw new DomainConflictException("kyc", "KYC verification already exists for this customer.");

            existing.Resubmit(document.Id.Value);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new SubmitKycResponse(existing.Id.Value, existing.Status.ToString(), existing.Version);
        }

        var kyc = KycVerification.Create(customerId);
        kyc.Submit(document.Id.Value);

        await kycRepo.AddAsync(kyc, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SubmitKycResponse(kyc.Id.Value, kyc.Status.ToString(), kyc.Version);
    }
}

using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.DocumentAggregate;
using MiniBank.Domain.DocumentAggregate.ValueObjects;
using MiniBank.Domain.KycAggregate;
using MiniBank.Features;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Kyc.SubmitCurrentKyc;

/// <summary>Submits KYC for the caller — CustomerId is the token subject, never client input.</summary>
internal sealed class SubmitCurrentKycHandler(
    IKycRepository kycRepo,
    IDocumentRepository documents,
    IAccessGuard accessGuard,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser) : ICommandHandler<SubmitCurrentKycCommand, SubmitCurrentKycResponse>
{
    public async Task<SubmitCurrentKycResponse> HandleAsync(SubmitCurrentKycCommand command, CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.UserId;

        var existing = await kycRepo.GetByCustomerIdAsync(customerId, cancellationToken);
        // IDOR fix: PrimaryDocumentId must exist and belong to the caller.
        var document = await documents.GetByIdAsync(new DocumentId(command.PrimaryDocumentId), cancellationToken)
            ?? throw new NotFoundException("document", command.PrimaryDocumentId);
        await accessGuard.EnsureDocumentOwnershipAsync(command.PrimaryDocumentId, cancellationToken);

        if (existing is not null)
        {
            if (existing.Status != KycStatus.Rejected)
                throw new DomainConflictException("kyc", "KYC verification already exists for this customer.");

            existing.Resubmit(document.Id.Value);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new SubmitCurrentKycResponse(existing.Id.Value, existing.Status.ToString(), existing.Version);
        }

        var kyc = KycVerification.Create(customerId);
        kyc.Submit(document.Id.Value);

        await kycRepo.AddAsync(kyc, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SubmitCurrentKycResponse(kyc.Id.Value, kyc.Status.ToString(), kyc.Version);
    }
}

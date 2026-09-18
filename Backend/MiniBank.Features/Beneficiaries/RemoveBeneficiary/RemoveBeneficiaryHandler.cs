using MiniBank.Abstractions;
using MiniBank.Domain.BeneficiaryAggregate.ValueObjects;
using MiniBank.Domain.BeneficiaryAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Beneficiaries.RemoveBeneficiary;

internal sealed class RemoveBeneficiaryHandler(
    IBeneficiaryRepository beneficiaries,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveBeneficiaryCommand, RemoveBeneficiaryResponse>
{
    public async Task<RemoveBeneficiaryResponse> HandleAsync(RemoveBeneficiaryCommand command, CancellationToken cancellationToken = default)
    {
        var beneficiary = await beneficiaries.GetByIdAsync(new BeneficiaryId(command.BeneficiaryId), cancellationToken)
            ?? throw new NotFoundException("beneficiary", command.BeneficiaryId);

        if (!beneficiary.OwnerCustomerId.Value.Equals(currentUser.UserId) && !currentUser.IsAdmin)
            throw new ForbiddenException("beneficiary", "Beneficiary is not owned by the current user.");

        await beneficiaries.RemoveAsync(beneficiary, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RemoveBeneficiaryResponse(beneficiary.Id.Value);
    }
}

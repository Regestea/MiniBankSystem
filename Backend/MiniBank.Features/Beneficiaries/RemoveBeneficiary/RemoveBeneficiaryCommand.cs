using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Beneficiaries.RemoveBeneficiary;

public sealed record RemoveBeneficiaryCommand(Guid BeneficiaryId) : ICommand<RemoveBeneficiaryResponse>;

public sealed record RemoveBeneficiaryResponse(Guid BeneficiaryId);

public sealed class RemoveBeneficiaryValidator : AbstractValidator<RemoveBeneficiaryCommand>
{
    public RemoveBeneficiaryValidator()
    {
        RuleFor(x => x.BeneficiaryId).NotEmpty();
    }
}

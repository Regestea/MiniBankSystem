using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Kyc.SubmitKyc;

public sealed record SubmitKycCommand(
    Guid CustomerId,
    Guid PrimaryDocumentId
) : ICommand<SubmitKycResponse>;

public sealed record SubmitKycResponse(
    Guid KycId,
    string Status,
    int Version);

public sealed class SubmitKycValidator : AbstractValidator<SubmitKycCommand>
{
    public SubmitKycValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.PrimaryDocumentId).NotEmpty();
    }
}

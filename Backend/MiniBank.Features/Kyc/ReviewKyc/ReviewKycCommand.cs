using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Kyc.ReviewKyc;

public sealed record ReviewKycCommand(
    Guid KycId,
    bool Approve,
    string? Reason
) : ICommand<ReviewKycResponse>;

public sealed record ReviewKycResponse(
    Guid KycId,
    string Status,
    int Version);

public sealed class ReviewKycValidator : AbstractValidator<ReviewKycCommand>
{
    public ReviewKycValidator()
    {
        RuleFor(x => x.KycId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required when rejecting.")
            .MaximumLength(500).When(x => !x.Approve);
    }
}

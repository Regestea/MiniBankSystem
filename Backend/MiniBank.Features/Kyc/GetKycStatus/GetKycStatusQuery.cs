using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Kyc.GetKycStatus;

public sealed record GetKycStatusQuery(Guid CustomerId) : IQuery<GetKycStatusResponse>;

public sealed class GetKycStatusQueryValidator : AbstractValidator<GetKycStatusQuery>
{
    public GetKycStatusQueryValidator()
        => RuleFor(x => x.CustomerId).NotEmpty();
}

public sealed record GetKycStatusResponse(
    Guid? KycId,
    string Status,
    Guid? PrimaryDocumentId,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason,
    DateTimeOffset CreatedAt);

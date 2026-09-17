using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Kyc.SubmitCurrentKyc;

/// <summary>
/// Submits KYC for the authenticated caller — no CustomerId in the contract.
/// </summary>
public sealed record SubmitCurrentKycCommand(
    Guid PrimaryDocumentId
) : ICommand<SubmitCurrentKycResponse>;

public sealed record SubmitCurrentKycResponse(
    Guid KycId,
    string Status,
    int Version);

public sealed class SubmitCurrentKycValidator : AbstractValidator<SubmitCurrentKycCommand>
{
    public SubmitCurrentKycValidator()
    {
        RuleFor(x => x.PrimaryDocumentId).NotEmpty();
    }
}

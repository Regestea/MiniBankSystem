using MiniBank.Features.Messaging;

namespace MiniBank.Features.Kyc.GetCurrentKycStatus;

/// <summary>
/// Returns the authenticated caller's own KYC status — no CustomerId in the contract.
/// </summary>
public sealed record GetCurrentKycStatusQuery : IQuery<GetCurrentKycStatusResponse>;

public sealed record GetCurrentKycStatusResponse(
    Guid? KycId,
    string Status,
    Guid? PrimaryDocumentId,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason,
    DateTimeOffset CreatedAt);

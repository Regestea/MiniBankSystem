using Dapper;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Kyc.GetCurrentKycStatus;

/// <summary>Reads the caller's own KYC row — identity from the token, no ownership guard needed.</summary>
internal sealed class GetCurrentKycStatusHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUserContext currentUser) : IQueryHandler<GetCurrentKycStatusQuery, GetCurrentKycStatusResponse>
{
    private const string Sql = """
        SELECT kyc_id              AS KycId,
               status              AS Status,
               primary_document_id AS PrimaryDocumentId,
               submitted_at        AS SubmittedAt,
               reviewed_at         AS ReviewedAt,
               rejection_reason    AS RejectionReason,
               created_at          AS CreatedAt
        FROM   kyc_verifications
        WHERE  customer_id = @CustomerId
        """;

    public async Task<GetCurrentKycStatusResponse> HandleAsync(GetCurrentKycStatusQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var row = await connection.QuerySingleOrDefaultAsync<KycRow>(
            new CommandDefinition(Sql, new { CustomerId = currentUser.UserId }, cancellationToken: cancellationToken));

        if (row is null)
            return new GetCurrentKycStatusResponse(null, "NotFound", null, null, null, null, DateTimeOffset.MinValue);

        return new GetCurrentKycStatusResponse(
            row.KycId, row.Status.ToString(), row.PrimaryDocumentId,
            row.SubmittedAt, row.ReviewedAt, row.RejectionReason, row.CreatedAt);
    }

    private sealed record KycRow(
        Guid KycId, short Status, Guid? PrimaryDocumentId,
        DateTimeOffset? SubmittedAt, DateTimeOffset? ReviewedAt,
        string? RejectionReason, DateTimeOffset CreatedAt);
}

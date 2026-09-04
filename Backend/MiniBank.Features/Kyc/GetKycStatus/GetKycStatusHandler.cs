using Dapper;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Kyc.GetKycStatus;

internal sealed class GetKycStatusHandler(
    ISqlConnectionFactory connectionFactory,
    IAccessGuard accessGuard) : IQueryHandler<GetKycStatusQuery, GetKycStatusResponse>
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

    public async Task<GetKycStatusResponse> HandleAsync(GetKycStatusQuery query, CancellationToken cancellationToken = default)
    {
        await accessGuard.EnsureCustomerOwnershipAsync(query.CustomerId, cancellationToken);

        using var connection = connectionFactory.CreateOpenConnection();

        var row = await connection.QuerySingleOrDefaultAsync<KycRow>(
            new CommandDefinition(Sql, new { query.CustomerId }, cancellationToken: cancellationToken));

        if (row is null)
            return new GetKycStatusResponse(null, "NotFound", null, null, null, null, DateTimeOffset.MinValue);

        return new GetKycStatusResponse(
            row.KycId, row.Status.ToString(), row.PrimaryDocumentId,
            row.SubmittedAt, row.ReviewedAt, row.RejectionReason, row.CreatedAt);
    }

    private sealed record KycRow(
        Guid KycId, short Status, Guid? PrimaryDocumentId,
        DateTimeOffset? SubmittedAt, DateTimeOffset? ReviewedAt,
        string? RejectionReason, DateTimeOffset CreatedAt);
}

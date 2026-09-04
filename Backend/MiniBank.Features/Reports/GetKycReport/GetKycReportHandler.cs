using Dapper;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Reports.GetKycReport;

internal sealed class GetKycReportHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<GetKycReportQuery, KycReportResponse>
{
    private const string Sql = """
        SELECT (SELECT COUNT(*) FROM kyc_verifications)              AS TotalVerifications,
               (SELECT COUNT(*) FROM kyc_verifications WHERE status = 0) AS Pending,
               (SELECT COUNT(*) FROM kyc_verifications WHERE status = 1) AS Submitted,
               (SELECT COUNT(*) FROM kyc_verifications WHERE status = 2) AS Approved,
               (SELECT COUNT(*) FROM kyc_verifications WHERE status = 3) AS Rejected,
               (SELECT COUNT(*) FROM documents)                      AS DocumentsUploaded,
               (SELECT COUNT(*) FROM documents WHERE status = 1)     AS DocumentsVerified,
               (SELECT COUNT(*) FROM documents WHERE status = 2)     AS DocumentsRejected;
        """;

    public async Task<KycReportResponse> HandleAsync(GetKycReportQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var row = await connection.QuerySingleAsync<KycReportResponse>(
            new CommandDefinition(Sql, cancellationToken: cancellationToken));

        return row;
    }
}

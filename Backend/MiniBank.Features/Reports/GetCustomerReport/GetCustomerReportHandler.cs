using Dapper;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Reports.GetCustomerReport;

internal sealed class GetCustomerReportHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<GetCustomerReportQuery, CustomerReportResponse>
{
    private const string Sql = """
        SELECT (SELECT COUNT(*) FROM customers)                              AS TotalCustomers,
               (SELECT COUNT(*) FROM customers WHERE status = 0)             AS PendingCustomers,
               (SELECT COUNT(*) FROM customers WHERE status = 1)             AS VerifiedCustomers,
               (SELECT COUNT(*) FROM customers WHERE status = 2)             AS BlockedCustomers,
               (SELECT COUNT(*) FROM kyc_verifications WHERE status = 0)     AS KycPending,
               (SELECT COUNT(*) FROM kyc_verifications WHERE status = 1)     AS KycSubmitted,
               (SELECT COUNT(*) FROM kyc_verifications WHERE status = 2)     AS KycApproved,
               (SELECT COUNT(*) FROM kyc_verifications WHERE status = 3)     AS KycRejected;
        """;

    public async Task<CustomerReportResponse> HandleAsync(GetCustomerReportQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var row = await connection.QuerySingleAsync<CustomerReportResponse>(
            new CommandDefinition(Sql, cancellationToken: cancellationToken));

        return row;
    }
}

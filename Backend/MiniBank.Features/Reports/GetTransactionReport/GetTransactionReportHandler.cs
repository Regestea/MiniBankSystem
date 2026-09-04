using Dapper;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Reports.GetTransactionReport;

internal sealed class GetTransactionReportHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<GetTransactionReportQuery, TransactionReportResponse>
{
    private const string Sql = """
        SELECT (SELECT COUNT(*) FROM transactions
                WHERE (@From::timestamptz IS NULL OR occurred_on >= @From)
                  AND (@To::timestamptz IS NULL   OR occurred_on < @To))              AS TotalTransactions,
               (SELECT COALESCE(SUM(amount), 0) FROM transactions WHERE type = 0
                AND (@From::timestamptz IS NULL OR occurred_on >= @From)
                AND (@To::timestamptz IS NULL   OR occurred_on < @To))              AS TotalDeposits,
               (SELECT COALESCE(SUM(amount), 0) FROM transactions WHERE type = 1
                AND (@From::timestamptz IS NULL OR occurred_on >= @From)
                AND (@To::timestamptz IS NULL   OR occurred_on < @To))              AS TotalWithdrawals,
               (SELECT COALESCE(SUM(amount), 0) FROM transactions WHERE type = 2
                AND (@From::timestamptz IS NULL OR occurred_on >= @From)
                AND (@To::timestamptz IS NULL   OR occurred_on < @To))              AS TotalTransfers,
               (SELECT COUNT(*) FROM transactions WHERE occurred_on::date = CURRENT_DATE) AS TransactionsToday,
               (SELECT COALESCE(SUM(amount), 0) FROM transactions WHERE occurred_on::date = CURRENT_DATE) AS VolumeToday;
        """;

    public async Task<TransactionReportResponse> HandleAsync(GetTransactionReportQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var row = await connection.QuerySingleAsync<TransactionReportResponse>(
            new CommandDefinition(Sql, new { query.From, query.To }, cancellationToken: cancellationToken));

        return row;
    }
}

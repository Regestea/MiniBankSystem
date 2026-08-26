using Dapper;
using MiniBank.Features.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Reports.GetBankReport;

// Admin report — computed from the immutable ledger, not from any cached balance
internal sealed class GetBankReportHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<GetBankReportQuery, BankReportResponse>
{
    private const string Sql = """
        SELECT (SELECT COUNT(*) FROM customers)                                        AS customers,
               (SELECT COUNT(*) FROM accounts)                                         AS accounts,
               (SELECT COUNT(*) FROM accounts WHERE status = 0)                        AS active_accounts,
               (SELECT COALESCE(SUM(CASE WHEN type IN (0, 2) THEN amount ELSE -amount END), 0)
                  FROM ledger_entries)                                                 AS total_balance;
        """;

    public async Task<BankReportResponse> Handle(GetBankReportQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var row = await connection.QuerySingleAsync<BankReportResponse>(
            new CommandDefinition(Sql, cancellationToken: cancellationToken));

        return row;
    }
}

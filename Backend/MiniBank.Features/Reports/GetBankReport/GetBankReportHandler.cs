using Dapper;
using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.Ledger;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Reports.GetBankReport;

internal sealed class GetBankReportHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<GetBankReportQuery, BankReportResponse>
{
    // Values resolved from the domain enums (not hardcoded numbers)
    private static readonly int[] CreditTypes =
        [(int)LedgerEntryType.Deposit, (int)LedgerEntryType.TransferIn];

    private const string Sql = """
        SELECT (SELECT COUNT(*) FROM customers)                                        AS customers,
               (SELECT COUNT(*) FROM accounts)                                         AS accounts,
               (SELECT COUNT(*) FROM accounts WHERE status = @ActiveStatus)            AS active_accounts,
               (SELECT COALESCE(SUM(CASE WHEN type = ANY(@CreditTypes) THEN amount ELSE -amount END), 0)
                  FROM ledger_entries)                                                 AS total_balance;
        """;

    public async Task<BankReportResponse> HandleAsync(GetBankReportQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var row = await connection.QuerySingleAsync<BankReportResponse>(
            new CommandDefinition(Sql, new { CreditTypes, ActiveStatus = (int)AccountStatus.Active },
                                  cancellationToken: cancellationToken));

        return row;
    }
}

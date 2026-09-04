using Dapper;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Risk.ListHighRiskCustomers;

internal sealed class ListHighRiskCustomersHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<ListHighRiskCustomersQuery, ListHighRiskCustomersResponse>
{
    private const string Sql = """
        SELECT risk_id                         AS RiskId,
               customer_id                     AS CustomerId,
               risk_level                      AS RiskLevel,
               daily_transaction_limit         AS DailyTransactionLimit,
               transactions_today              AS TransactionsToday,
               amount_today                    AS AmountToday
        FROM   customer_risks
        WHERE  risk_level >= @MinLevel
        ORDER BY risk_level DESC, amount_today DESC
        LIMIT 100
        """;

    public async Task<ListHighRiskCustomersResponse> HandleAsync(ListHighRiskCustomersQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        // Clamp to valid RiskLevel range (Low=0..High=2). Default 1 = Medium and above.
        // Without clamping, negative MinScore would dump the whole table; without LIMIT
        // an admin poll could exfiltrate/scan unbounded rows.
        var minLevel = Math.Clamp(query.MinScore ?? 1, 0, 2);

        var rows = await connection.QueryAsync<RiskRow>(
            new CommandDefinition(Sql, new { MinLevel = minLevel }, cancellationToken: cancellationToken));

        var items = rows.Select(r => new HighRiskCustomerItem(
            r.RiskId, r.CustomerId, r.RiskLevel.ToString(),
            r.DailyTransactionLimit, r.TransactionsToday, r.AmountToday)).ToList();

        return new ListHighRiskCustomersResponse(items);
    }

    private sealed record RiskRow(
        Guid RiskId, Guid CustomerId, short RiskLevel,
        decimal DailyTransactionLimit, int TransactionsToday, decimal AmountToday);
}

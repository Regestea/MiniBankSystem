using Dapper;
using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Risk.GetCustomerRisk;

internal sealed class GetCustomerRiskHandler(
    ISqlConnectionFactory connectionFactory,
    IAccessGuard accessGuard) : IQueryHandler<GetCustomerRiskQuery, GetCustomerRiskResponse>
{
    private const string Sql = """
        SELECT risk_id                         AS RiskId,
               customer_id                     AS CustomerId,
               risk_level                      AS RiskLevel,
               daily_transaction_limit         AS DailyTransactionLimit,
               daily_transaction_count_limit   AS DailyTransactionCountLimit,
               transactions_today              AS TransactionsToday,
               amount_today                    AS AmountToday
        FROM   customer_risks
        WHERE  customer_id = @CustomerId
        """;

    public async Task<GetCustomerRiskResponse> HandleAsync(GetCustomerRiskQuery query, CancellationToken cancellationToken = default)
    {
        await accessGuard.EnsureRiskOwnershipAsync(query.CustomerId, cancellationToken);

        using var connection = connectionFactory.CreateOpenConnection();

        var row = await connection.QuerySingleOrDefaultAsync<RiskRow>(
            new CommandDefinition(Sql, new { query.CustomerId }, cancellationToken: cancellationToken));

        if (row is null)
            throw new NotFoundException("customer_risk", query.CustomerId);

        return new GetCustomerRiskResponse(
            row.RiskId, row.CustomerId, row.RiskLevel.ToString(),
            row.DailyTransactionLimit, row.DailyTransactionCountLimit,
            row.TransactionsToday, row.AmountToday);
    }

    private sealed record RiskRow(
        Guid RiskId, Guid CustomerId, short RiskLevel,
        decimal DailyTransactionLimit, int DailyTransactionCountLimit,
        int TransactionsToday, decimal AmountToday);
}

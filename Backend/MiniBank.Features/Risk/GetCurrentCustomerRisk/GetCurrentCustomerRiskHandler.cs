using Dapper;
using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Messaging;
using MiniBank.Features.Risk.GetCustomerRisk;

namespace MiniBank.Features.Risk.GetCurrentCustomerRisk;

/// <summary>Reads the caller's own risk row — identity from the token, no ownership guard needed.</summary>
internal sealed class GetCurrentCustomerRiskHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUserContext currentUser) : IQueryHandler<GetCurrentCustomerRiskQuery, GetCustomerRiskResponse>
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

    public async Task<GetCustomerRiskResponse> HandleAsync(GetCurrentCustomerRiskQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var row = await connection.QuerySingleOrDefaultAsync<RiskRow>(
            new CommandDefinition(Sql, new { CustomerId = currentUser.UserId }, cancellationToken: cancellationToken));

        if (row is null)
            throw new NotFoundException("customer_risk", currentUser.UserId);

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

using Dapper;
using MiniBank.Abstractions;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Transactions.ListMyTransactions;

internal sealed class ListMyTransactionsHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUserContext currentUser) : IQueryHandler<ListMyTransactionsQuery, MyTransactionsResponse>
{
    private const string ItemsSql = """
        SELECT t.transaction_id        AS TransactionId,
               t.type                  AS Type,
               t.amount                AS Amount,
               sa.account_number       AS SourceAccountNumber,
               da.account_number       AS DestinationAccountNumber,
               t.occurred_on           AS OccurredOn,
               t.reference_id          AS ReferenceId,
               t.description           AS Description
        FROM   transactions t
        LEFT   JOIN accounts sa ON sa.account_id = t.source_account_id
        LEFT   JOIN accounts da ON da.account_id = t.destination_account_id
        WHERE  t.source_account_id IN (SELECT account_id FROM accounts WHERE customer_id = @UserId)
           OR  t.destination_account_id IN (SELECT account_id FROM accounts WHERE customer_id = @UserId)
        ORDER  BY t.occurred_on DESC, t.transaction_id DESC
        OFFSET @Offset LIMIT @Limit
        """;

    private const string CountSql = """
        SELECT COUNT(*)
        FROM   transactions t
        WHERE  t.source_account_id IN (SELECT account_id FROM accounts WHERE customer_id = @UserId)
           OR  t.destination_account_id IN (SELECT account_id FROM accounts WHERE customer_id = @UserId)
        """;

    public async Task<MyTransactionsResponse> HandleAsync(ListMyTransactionsQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var items = (await connection.QueryAsync<MyTransactionRow>(
            new CommandDefinition(ItemsSql,
                new { UserId = currentUser.UserId, Offset = (query.Page - 1) * query.PageSize, Limit = query.PageSize },
                cancellationToken: cancellationToken))).ToList();

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(CountSql, new { UserId = currentUser.UserId }, cancellationToken: cancellationToken));

        return new MyTransactionsResponse(
            query.Page, query.PageSize, total,
            items.Select(r => new MyTransactionDto(
                r.TransactionId, ((TransactionType)r.Type).ToString(),
                r.Amount, r.SourceAccountNumber, r.DestinationAccountNumber,
                DbTime.Utc(r.OccurredOn), r.ReferenceId, r.Description)).ToList());
    }

    private sealed record MyTransactionRow(
        Guid TransactionId, short Type, decimal Amount,
        string? SourceAccountNumber, string? DestinationAccountNumber,
        DateTime OccurredOn, string ReferenceId, string? Description);
}

using Dapper;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.GetStatement;

/// <summary>Statement query — Dapper read side.</summary>
internal sealed class GetStatementHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<GetStatementQuery, StatementResponse>
{
    private const string AccountSql = """
        SELECT a.account_id, a.account_number, a.status,
               COALESCE(SUM(CASE WHEN e.type IN (0, 2) THEN e.amount ELSE -e.amount END), 0) AS balance
        FROM   accounts a
        LEFT   JOIN ledger_entries e ON e.account_id = a.account_id
        WHERE  a.account_id = @AccountId AND a.customer_id = @RequesterUserId
        GROUP  BY a.account_id, a.account_number, a.status
        """;

    private const string EntriesSql = """
        SELECT ledger_entry_id, type, amount, occurred_on, reference_id, description
        FROM   ledger_entries
        WHERE  account_id = @AccountId
        ORDER  BY occurred_on, ledger_entry_id
        """;

    public async Task<StatementResponse> HandleAsync(GetStatementQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var account = await connection.QuerySingleOrDefaultAsync<AccountRow>(
            new CommandDefinition(AccountSql, new { query.AccountId, query.RequesterUserId },
                                  cancellationToken: cancellationToken));

        if (account is null)
        {
            var exists = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition("SELECT COUNT(1) FROM accounts WHERE account_id = @AccountId",
                    new { query.AccountId }, cancellationToken: cancellationToken));

            throw exists
                ? new Domain.BuildingBlocks.Exceptions.ForbiddenException("account", "Account is not owned by the current user.")
                : new Domain.BuildingBlocks.Exceptions.NotFoundException("account", query.AccountId);
        }

        var entries = (await connection.QueryAsync<StatementEntryDto>(
            new CommandDefinition(EntriesSql, new { query.AccountId },
                                  cancellationToken: cancellationToken))).ToList();

        return new StatementResponse(query.AccountId, account.AccountNumber, MapStatus(account.Status),
                                     account.Balance, entries);
    }

    private static string MapStatus(short status) => status switch
    {
        0 => "Active",
        1 => "Frozen",
        2 => "Closed",
        _ => status.ToString()
    };

    private sealed record AccountRow(Guid AccountId, string AccountNumber, short Status, decimal Balance);
}

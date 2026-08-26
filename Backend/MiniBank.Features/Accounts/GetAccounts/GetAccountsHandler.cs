using Dapper;
using MiniBank.Features.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.GetAccounts;

// Read side — Dapper: accounts of the logged-in user with derived balance
// Filter is automatic for User role (my accounts). Admin calling same endpoint sees own accounts only.
internal sealed class GetAccountsHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<GetAccountsQuery, IReadOnlyList<AccountDto>>
{
    private const string Sql = """
        SELECT a.account_id,
               a.account_number,
               a.account_type,
               a.status,
               COALESCE(SUM(CASE WHEN e.type IN (0, 2) THEN e.amount ELSE -e.amount END), 0) AS balance,
               a.created_at
        FROM   accounts a
        JOIN   "AspNetUsers" u ON u.customer_id = a.customer_id
        LEFT   JOIN ledger_entries e ON e.account_id = a.account_id
        WHERE  u.Id = @UserId
        GROUP  BY a.account_id, a.account_number, a.account_type, a.status, a.created_at
        ORDER  BY a.created_at;
        """;

    public async Task<IReadOnlyList<AccountDto>> Handle(GetAccountsQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var rows = await connection.QueryAsync<AccountDto>(
            new CommandDefinition(Sql, new { query.UserId }, cancellationToken: cancellationToken));

        return rows.ToList();
    }
}

using Dapper;
using MiniBank.Abstractions;
using MiniBank.Domain.Ledger;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.GetAccounts;

internal sealed class GetAccountsHandler(ISqlConnectionFactory connectionFactory, ICurrentUserContext currentUser)
    : IQueryHandler<GetAccountsQuery, IReadOnlyList<AccountDto>>
{
    // Credit types resolved from the enum (not hardcoded numbers) and passed as SQL parameters
    private static readonly int[] CreditTypes =
        [(int)LedgerEntryType.Deposit, (int)LedgerEntryType.TransferIn];

    private const string Sql = """
        SELECT a.account_id,
               a.account_number,
               a.account_type,
               a.status,
               COALESCE(SUM(CASE WHEN e.type = ANY(@CreditTypes) THEN e.amount ELSE -e.amount END), 0) AS balance,
               a.created_at
        FROM   accounts a
        LEFT   JOIN ledger_entries e ON e.account_id = a.account_id
        WHERE  a.customer_id = @UserId
        GROUP  BY a.account_id, a.account_number, a.account_type, a.status, a.created_at
        ORDER  BY a.created_at;
        """;

    public async Task<IReadOnlyList<AccountDto>> HandleAsync(GetAccountsQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var rows = await connection.QueryAsync<AccountDto>(
            new CommandDefinition(Sql, new { UserId = currentUser.UserId, CreditTypes }, cancellationToken: cancellationToken));

        return rows.ToList();
    }
}

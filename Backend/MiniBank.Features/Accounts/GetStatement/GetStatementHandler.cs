using Dapper;
using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.Ledger;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.GetStatement;

/// <summary>Statement query — Dapper read side.</summary>
internal sealed class GetStatementHandler(ISqlConnectionFactory connectionFactory, ICurrentUserContext currentUser)
    : IQueryHandler<GetStatementQuery, StatementResponse>
{
    // Credit types resolved from the enum (not hardcoded numbers) and passed as SQL parameters
    private static readonly int[] CreditTypes =
        [(int)LedgerEntryType.Deposit, (int)LedgerEntryType.TransferIn];

    private const string AccountSql = """
        SELECT a.account_id, a.account_number, a.status,
               COALESCE(SUM(CASE WHEN e.type = ANY(@CreditTypes) THEN e.amount ELSE -e.amount END), 0) AS balance
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
        OFFSET @Offset LIMIT @Limit;

        SELECT COUNT(*) FROM ledger_entries WHERE account_id = @AccountId;
        """;

    public async Task<StatementResponse> HandleAsync(GetStatementQuery query, CancellationToken cancellationToken = default)
    {
        // Fail-fast on invalid paging: validator rejects Page < 1 / PageSize outside 1..100.
        // No silent Clamp — callers get 400 instead of a coerced page.
        var page = query.Page;
        var pageSize = query.PageSize;

        using var connection = connectionFactory.CreateOpenConnection();

        var account = await connection.QuerySingleOrDefaultAsync<AccountRow>(
            new CommandDefinition(AccountSql, new { query.AccountId, RequesterUserId = currentUser.UserId, CreditTypes },
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

        await using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            EntriesSql,
            new { query.AccountId, Offset = (page - 1) * pageSize, Limit = pageSize },
            cancellationToken: cancellationToken));

        var entries = (await multi.ReadAsync<StatementEntryDto>()).ToList();
        var total = await multi.ReadSingleAsync<int>();

        // Status mapped via the domain enum instead of hardcoded numbers
        var status = ((AccountStatus)account.Status).ToString();

        return new StatementResponse(query.AccountId, account.AccountNumber, status, account.Balance,
                                     page, pageSize, total, entries);
    }

    private sealed record AccountRow(Guid AccountId, string AccountNumber, short Status, decimal Balance);
}

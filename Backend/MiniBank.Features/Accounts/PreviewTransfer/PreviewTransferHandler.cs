using Dapper;
using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.PreviewTransfer;

/// <summary>
/// Resolves the destination account holder's name.
/// Step 2 of the transfer flow shows the FULL name for explicit confirmation;
/// the masked variant stays for list-style UIs. Never returns balances, emails or phones.
/// </summary>
internal sealed class PreviewTransferHandler(
    ISqlConnectionFactory connectionFactory) : IQueryHandler<PreviewTransferQuery, PreviewTransferResponse>
{
    private const string Sql = """
        SELECT a.account_id     AS AccountId,
               a.account_number AS AccountNumber,
               c.full_name      AS FullName
        FROM   accounts a
        JOIN   customers c ON c.customer_id = a.customer_id
        WHERE  a.account_id = @ToAccountId
        """;

    public async Task<PreviewTransferResponse> HandleAsync(PreviewTransferQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var row = await connection.QuerySingleOrDefaultAsync<PreviewRow>(
            new CommandDefinition(Sql, new { query.ToAccountId }, cancellationToken: cancellationToken));

        if (row is null)
            throw new NotFoundException("account", query.ToAccountId);

        return new PreviewTransferResponse(row.AccountId, row.AccountNumber, row.FullName, Mask(row.FullName));
    }

    internal static string Mask(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "***";
        return string.Join(' ', parts.Select(p => $"{p[0]}***"));
    }

    private sealed record PreviewRow(Guid AccountId, string AccountNumber, string FullName);
}

/// <summary>Same preview resolved by account number (IR-... or 16-digit, case-insensitive).</summary>
internal sealed class PreviewTransferByNumberHandler(
    ISqlConnectionFactory connectionFactory) : IQueryHandler<PreviewTransferByNumberQuery, PreviewTransferResponse>
{
    private const string Sql = """
        SELECT a.account_id     AS AccountId,
               a.account_number AS AccountNumber,
               c.full_name      AS FullName
        FROM   accounts a
        JOIN   customers c ON c.customer_id = a.customer_id
        WHERE  UPPER(a.account_number) = UPPER(@AccountNumber)
        """;

    public async Task<PreviewTransferResponse> HandleAsync(PreviewTransferByNumberQuery query, CancellationToken cancellationToken = default)
    {
        var normalized = MiniBank.Domain.AccountAggregate.ValueObjects.AccountNumber.Normalize(query.AccountNumber);

        using var connection = connectionFactory.CreateOpenConnection();

        var row = await connection.QuerySingleOrDefaultAsync<PreviewRow>(
            new CommandDefinition(Sql, new { AccountNumber = normalized }, cancellationToken: cancellationToken));

        if (row is null)
            throw new NotFoundException("account", query.AccountNumber);

        return new PreviewTransferResponse(row.AccountId, row.AccountNumber, row.FullName, PreviewTransferHandler.Mask(row.FullName));
    }

    private sealed record PreviewRow(Guid AccountId, string AccountNumber, string FullName);
}

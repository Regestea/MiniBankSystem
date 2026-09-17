using Dapper;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Documents.ListCurrentCustomerDocuments;

/// <summary>Lists the caller's own documents — identity from the token, no ownership guard needed.</summary>
internal sealed class ListCurrentCustomerDocumentsHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUserContext currentUser) : IQueryHandler<ListCurrentCustomerDocumentsQuery, ListCurrentCustomerDocumentsResponse>
{
    private const string Sql = """
        SELECT document_id    AS DocumentId,
               file_name      AS FileName,
               type           AS Type,
               status         AS Status,
               created_at     AS CreatedAt
        FROM   documents
        WHERE  customer_id = @CustomerId
        ORDER BY created_at DESC
        """;

    public async Task<ListCurrentCustomerDocumentsResponse> HandleAsync(ListCurrentCustomerDocumentsQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var rows = await connection.QueryAsync<DocumentRow>(
            new CommandDefinition(Sql, new { CustomerId = currentUser.UserId }, cancellationToken: cancellationToken));

        var items = rows.Select(r => new DocumentListItem(
            r.DocumentId, r.FileName, r.Type.ToString(), r.Status.ToString(), r.CreatedAt)).ToList();

        return new ListCurrentCustomerDocumentsResponse(items);
    }

    private sealed record DocumentRow(
        Guid DocumentId, string FileName, short Type, short Status, DateTimeOffset CreatedAt);
}

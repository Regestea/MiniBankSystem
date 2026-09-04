using Dapper;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Documents.ListCustomerDocuments;

internal sealed class ListCustomerDocumentsHandler(
    ISqlConnectionFactory connectionFactory,
    IAccessGuard accessGuard) : IQueryHandler<ListCustomerDocumentsQuery, ListCustomerDocumentsResponse>
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

    public async Task<ListCustomerDocumentsResponse> HandleAsync(ListCustomerDocumentsQuery query, CancellationToken cancellationToken = default)
    {
        await accessGuard.EnsureCustomerOwnershipAsync(query.CustomerId, cancellationToken);

        using var connection = connectionFactory.CreateOpenConnection();

        var rows = await connection.QueryAsync<DocumentRow>(
            new CommandDefinition(Sql, new { query.CustomerId }, cancellationToken: cancellationToken));

        var items = rows.Select(r => new DocumentListItem(
            r.DocumentId, r.FileName, r.Type.ToString(), r.Status.ToString(), r.CreatedAt)).ToList();

        return new ListCustomerDocumentsResponse(items);
    }

    private sealed record DocumentRow(
        Guid DocumentId, string FileName, short Type, short Status, DateTimeOffset CreatedAt);
}

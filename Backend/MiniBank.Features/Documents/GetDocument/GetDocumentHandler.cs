using Dapper;
using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Documents.GetDocument;

internal sealed class GetDocumentHandler(
    ISqlConnectionFactory connectionFactory,
    IAccessGuard accessGuard) : IQueryHandler<GetDocumentQuery, GetDocumentResponse>
{
    private const string Sql = """
        SELECT document_id    AS DocumentId,
               customer_id    AS CustomerId,
               file_name      AS FileName,
               content_type   AS ContentType,
               file_size      AS FileSize,
               type           AS Type,
               status         AS Status,
               rejection_reason AS RejectionReason,
               verified_by    AS VerifiedBy,
               verified_at    AS VerifiedAt,
               created_at     AS CreatedAt
        FROM   documents
        WHERE  document_id = @DocumentId
        """;

    public async Task<GetDocumentResponse> HandleAsync(GetDocumentQuery query, CancellationToken cancellationToken = default)
    {
        await accessGuard.EnsureDocumentOwnershipAsync(query.DocumentId, cancellationToken);

        using var connection = connectionFactory.CreateOpenConnection();

        var row = await connection.QuerySingleOrDefaultAsync<DocumentRow>(
            new CommandDefinition(Sql, new { query.DocumentId }, cancellationToken: cancellationToken));

        if (row is null)
            throw new NotFoundException("document", query.DocumentId);

        return new GetDocumentResponse(
            row.DocumentId, row.CustomerId, row.FileName, row.ContentType,
            row.FileSize, row.Type.ToString(), row.Status.ToString(),
            row.RejectionReason, row.VerifiedBy, row.VerifiedAt, row.CreatedAt);
    }

    private sealed record DocumentRow(
        Guid DocumentId, Guid CustomerId, string FileName, string ContentType,
        long FileSize, short Type, short Status, string? RejectionReason,
        Guid? VerifiedBy, DateTimeOffset? VerifiedAt, DateTimeOffset CreatedAt);
}

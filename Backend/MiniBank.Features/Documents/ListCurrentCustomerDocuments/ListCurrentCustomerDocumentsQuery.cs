using MiniBank.Features.Messaging;

namespace MiniBank.Features.Documents.ListCurrentCustomerDocuments;

/// <summary>
/// Lists the authenticated caller's own documents — no CustomerId in the contract.
/// </summary>
public sealed record ListCurrentCustomerDocumentsQuery : IQuery<ListCurrentCustomerDocumentsResponse>;

public sealed record ListCurrentCustomerDocumentsResponse(IReadOnlyList<DocumentListItem> Documents);

public sealed record DocumentListItem(
    Guid DocumentId,
    string FileName,
    string Type,
    string Status,
    DateTimeOffset CreatedAt);

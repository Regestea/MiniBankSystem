using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Documents.ListCustomerDocuments;

public sealed record ListCustomerDocumentsQuery(Guid CustomerId) : IQuery<ListCustomerDocumentsResponse>;

public sealed class ListCustomerDocumentsQueryValidator : AbstractValidator<ListCustomerDocumentsQuery>
{
    public ListCustomerDocumentsQueryValidator()
        => RuleFor(x => x.CustomerId).NotEmpty();
}

public sealed record ListCustomerDocumentsResponse(IReadOnlyList<DocumentListItem> Documents);

public sealed record DocumentListItem(
    Guid DocumentId,
    string FileName,
    string Type,
    string Status,
    DateTimeOffset CreatedAt);

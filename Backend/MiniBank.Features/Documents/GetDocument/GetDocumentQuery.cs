using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Documents.GetDocument;

public sealed record GetDocumentQuery(Guid DocumentId) : IQuery<GetDocumentResponse>;

public sealed class GetDocumentQueryValidator : AbstractValidator<GetDocumentQuery>
{
    public GetDocumentQueryValidator()
        => RuleFor(x => x.DocumentId).NotEmpty();
}

public sealed record GetDocumentResponse(
    Guid DocumentId,
    Guid CustomerId,
    string FileName,
    string ContentType,
    long FileSize,
    string Type,
    string Status,
    string? RejectionReason,
    Guid? VerifiedBy,
    DateTimeOffset? VerifiedAt,
    DateTimeOffset CreatedAt);

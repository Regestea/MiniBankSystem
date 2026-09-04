using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Documents.VerifyDocument;

public sealed record VerifyDocumentCommand(
    Guid DocumentId,
    bool Approve,
    string? Reason
) : ICommand<VerifyDocumentResponse>;

public sealed record VerifyDocumentResponse(
    Guid DocumentId,
    string Status,
    int Version);

public sealed class VerifyDocumentValidator : AbstractValidator<VerifyDocumentCommand>
{
    public VerifyDocumentValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required when rejecting.")
            .MaximumLength(500).When(x => !x.Approve);
    }
}

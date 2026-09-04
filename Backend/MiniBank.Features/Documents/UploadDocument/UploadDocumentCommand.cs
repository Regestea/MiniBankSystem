using FluentValidation;
using MiniBank.Domain.DocumentAggregate;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Documents.UploadDocument;

public sealed record UploadDocumentCommand(
    string FileName,
    string ContentType,
    long FileSize,
    DocumentType Type
) : ICommand<UploadDocumentResponse>;

public sealed record UploadDocumentResponse(
    Guid DocumentId,
    string FileName,
    string Status,
    int Version);

public sealed class UploadDocumentValidator : AbstractValidator<UploadDocumentCommand>
{
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "application/pdf"];

    private static readonly string[] AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".pdf"];

    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public UploadDocumentValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255)
            .Must(name =>
            {
                var ext = System.IO.Path.GetExtension(name)?.ToLowerInvariant();
                return AllowedExtensions.Contains(ext);
            })
            .WithMessage($"Only .jpg, .jpeg, .png, and .pdf files are allowed. Received: {{PropertyValue}}");

        RuleFor(x => x.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage($"Only JPEG, PNG, and PDF are allowed. Received: {{PropertyValue}}");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File size must be positive.")
            .LessThanOrEqualTo(MaxFileSize).WithMessage($"File size must be less than {MaxFileSize / 1024 / 1024}MB.");

        // Extension <-> MIME consistency (e.g. .jpg must not claim application/pdf).
        RuleFor(x => x)
            .Must(x =>
            {
                var ext = System.IO.Path.GetExtension(x.FileName)?.ToLowerInvariant();
                return (ext, x.ContentType) switch
                {
                    (".jpg" or ".jpeg", "image/jpeg") => true,
                    (".png", "image/png") => true,
                    (".pdf", "application/pdf") => true,
                    _ => false
                };
            })
            .WithMessage("File extension does not match ContentType.");
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Documents.ListCustomerDocuments;
using MiniBank.Features.Documents.UploadDocument;
using MiniBank.Features.Documents.VerifyDocument;
using MiniBank.Features.Documents.GetDocument;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers;

/// <summary>
/// Document management operations.
/// </summary>
[ApiController]
[Route("documents")]
[Authorize]
[Produces("application/json")]
public sealed class DocumentsController(IMediator mediator) : ControllerBase
{
    /// <summary>Uploads a document. [Authenticated]</summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadDocumentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<UploadDocumentResponse>> Upload(UploadDocumentCommand command, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetDocument), new { id = response.DocumentId }, response);
    }

    /// <summary>Gets document metadata. [Authenticated]</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetDocumentResponse>> GetDocument(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetDocumentQuery(id), cancellationToken));

    /// <summary>Lists customer documents. [Authenticated]</summary>
    [HttpGet("~/customers/{customerId:guid}/documents")]
    [ProducesResponseType(typeof(ListCustomerDocumentsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListCustomerDocumentsResponse>> ListCustomerDocuments(Guid customerId, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new ListCustomerDocumentsQuery(customerId), cancellationToken));
}

/// <summary>
/// Admin document operations.
/// </summary>
[ApiController]
[Route("admin/documents")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class AdminDocumentsController(IMediator mediator) : ControllerBase
{
    /// <summary>Verifies or rejects a document. [Admin]</summary>
    [HttpPost("{id:guid}/verify")]
    [ProducesResponseType(typeof(VerifyDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VerifyDocumentResponse>> VerifyDocument(Guid id, [FromBody] VerifyDocumentRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new VerifyDocumentCommand(id, request.Approve, request.Reason), cancellationToken));
}

public sealed record VerifyDocumentRequest(bool Approve, string? Reason);

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Documents.ListCurrentCustomerDocuments;
using MiniBank.Features.Documents.UploadDocument;
using MiniBank.Features.Documents.GetDocument;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers.Customer;

/// <summary>
/// Customer document self-service (REST resource: /documents).
/// Identity comes from the token — no customer id in any route.
/// Admin verification lives in Controllers/Admin/AdminDocumentsController.
/// </summary>
[ApiController]
[Route("documents")]
[Authorize]
[Produces("application/json")]
public sealed class DocumentsController(IMediator mediator) : ControllerBase
{
    /// <summary>Uploads a document for the caller.</summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadDocumentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<UploadDocumentResponse>> Upload(UploadDocumentCommand command, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetDocument), new { id = response.DocumentId }, response);
    }

    /// <summary>Lists the caller's own documents (identity from token).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListCurrentCustomerDocumentsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListCurrentCustomerDocumentsResponse>> ListMine(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new ListCurrentCustomerDocumentsQuery(), cancellationToken));

    /// <summary>Gets one of the caller's own documents (ownership enforced in handler).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetDocumentResponse>> GetDocument(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetDocumentQuery(id), cancellationToken));
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Documents.VerifyDocument;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers.Admin;

/// <summary>Admin document operations (REST resource: /admin/documents).</summary>
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

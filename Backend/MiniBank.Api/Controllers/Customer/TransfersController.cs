using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniBank.Features.Accounts;
using MiniBank.Features.Accounts.PreviewTransfer;
using MiniBank.Features.Accounts.Transfer;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers.Customer;

/// <summary>Customer transfers (REST resource: /transfers). Source must be owned by the caller; destination may be any active account.</summary>
[ApiController]
[Route("transfers")]
[Authorize]
public sealed class TransfersController(IMediator mediator) : ControllerBase
{
    /// <summary>Transfer between accounts. Source must be owned by the caller; destination may be any active account. Idempotent (same key+payload → 200, different payload → 409).</summary>
    [HttpPost]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransferResponse>> Transfer(TransferCommand command, CancellationToken cancellationToken)
        => Ok(await mediator.Send(command, cancellationToken));

    /// <summary>Previews a transfer destination — returns the masked holder name (e.g. "S*** A***") so the sender can verify the receiver without seeing full PII or balances.</summary>
    [HttpPost("preview")]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(PreviewTransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreviewTransferResponse>> Preview(PreviewTransferRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new PreviewTransferQuery(request.ToAccountId), cancellationToken));
}

public sealed record PreviewTransferRequest(Guid ToAccountId);

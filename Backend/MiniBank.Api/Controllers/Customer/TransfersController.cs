using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniBank.Features.Accounts;
using MiniBank.Features.Accounts.PreviewTransfer;
using MiniBank.Features.Accounts.Transfer;
using MiniBank.Features.Accounts.TransferByNumber;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers.Customer;

/// <summary>
/// Customer transfers (REST resource: /transfers). Source must be owned by the caller; destination may be any active account.
/// Flow with account numbers (USD amounts):
///   1. POST /transfers/preview-by-number { accountNumber } → full holder name (step 2: show name)
///   2. POST /transfers/by-account-number { fromAccountId, toAccountNumber, amount, saveBeneficiary } → confirm + transfer (step 3)
/// </summary>
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

    /// <summary>Transfer by destination account number (IR-XXXXXXXXXX or 16-digit, USD). Set saveBeneficiary to remember the destination for next transfers.</summary>
    [HttpPost("by-account-number")]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransferResponse>> TransferByNumber(TransferByAccountNumberCommand command, CancellationToken cancellationToken)
        => Ok(await mediator.Send(command, cancellationToken));

    /// <summary>Previews a transfer destination — returns the holder name (full + masked) so the sender can verify the receiver without seeing balances.</summary>
    [HttpPost("preview")]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(PreviewTransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreviewTransferResponse>> Preview(PreviewTransferRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new PreviewTransferQuery(request.ToAccountId), cancellationToken));

    /// <summary>Step 2 of the account-number flow: enter account number → see the holder's full name before confirming (USD amount is entered in step 1 by the client).</summary>
    [HttpPost("preview-by-number")]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(PreviewTransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreviewTransferResponse>> PreviewByNumber(PreviewByNumberRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new PreviewTransferByNumberQuery(request.AccountNumber), cancellationToken));
}

public sealed record PreviewTransferRequest(Guid ToAccountId);
public sealed record PreviewByNumberRequest(string AccountNumber);

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Accounts;
using MiniBank.Features.Accounts.Transfer;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers;

[ApiController]
[Route("transfers")]
[Authorize]
public sealed class TransfersController(IMediator mediator) : ControllerBase
{
    /// <summary>Double-entry transfer between two accounts owned by the caller.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransferResponse>> Transfer(TransferCommand command, CancellationToken cancellationToken)
        => Ok(await mediator.Send(command, cancellationToken));
}

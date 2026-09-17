using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniBank.Features.Accounts;
using MiniBank.Features.Accounts.ApproveAccount;
using MiniBank.Features.Accounts.FreezeAccount;
using MiniBank.Features.Accounts.RejectAccount;
using MiniBank.Features.Accounts.UnfreezeAccount;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers.Admin;

/// <summary>
/// Admin account management (REST resource: /admin/accounts).
/// Every action here requires the "Admin" role — enforced once at controller level.
/// </summary>
[ApiController]
[Route("admin/accounts")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin_endpoints")]
[Produces("application/json")]
public sealed class AdminAccountsController(IMediator mediator) : ControllerBase
{
    /// <summary>Approves a pending account. [Admin]</summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApproveAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApproveAccountResponse>> ApproveAccount(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new ApproveAccountCommand(id), cancellationToken));

    /// <summary>Rejects a pending account. [Admin]</summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(RejectAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RejectAccountResponse>> RejectAccount(Guid id, [FromBody] RejectAccountRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new RejectAccountCommand(id, request.Reason), cancellationToken));

    /// <summary>Freezes an account. [Admin]</summary>
    [HttpPost("{id:guid}/freeze")]
    [ProducesResponseType(typeof(AccountStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountStatusResponse>> FreezeAccount(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new FreezeAccountCommand(id), cancellationToken));

    /// <summary>Unfreezes a frozen account. [Admin]</summary>
    [HttpPost("{id:guid}/unfreeze")]
    [ProducesResponseType(typeof(AccountStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountStatusResponse>> UnfreezeAccount(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new UnfreezeAccountCommand(id), cancellationToken));
}

public sealed record RejectAccountRequest(string Reason);

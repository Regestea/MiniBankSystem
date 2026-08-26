using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Accounts;
using MiniBank.Features.Accounts.FreezeAccount;
using MiniBank.Features.Accounts.UnfreezeAccount;
using MiniBank.Features.Reports.GetBankReport;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public sealed class AdminController(IMediator mediator) : ControllerBase
{
    /// <summary>Admin — freeze an account: new transactions are rejected until unfrozen.</summary>
    [HttpPatch("accounts/{accountId:guid}/freeze")]
    public async Task<ActionResult<AccountStatusResponse>> Freeze([FromRoute] Guid accountId, CancellationToken ct)
        => Ok(await mediator.Send(new FreezeAccountCommand(accountId), ct));

    /// <summary>Admin — unfreeze a frozen account.</summary>
    [HttpPatch("accounts/{accountId:guid}/unfreeze")]
    public async Task<ActionResult<AccountStatusResponse>> Unfreeze([FromRoute] Guid accountId, CancellationToken ct)
        => Ok(await mediator.Send(new UnfreezeAccountCommand(accountId), ct));

    /// <summary>Admin — bank-wide report (counts + total balance derived from the ledger).</summary>
    [HttpGet("reports/bank")]
    public async Task<ActionResult<BankReportResponse>> BankReport(CancellationToken ct)
        => Ok(await mediator.Send(new GetBankReportQuery(), ct));
}

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
    /// <summary>Freeze account.</summary>
    [HttpPatch("accounts/{accountId:guid}/freeze")]
    public async Task<ActionResult<AccountStatusResponse>> Freeze([FromRoute] Guid accountId, CancellationToken ct)
        => Ok(await mediator.SendAsync(new FreezeAccountCommand(accountId), ct));

    /// <summary>Unfreeze account.</summary>
    [HttpPatch("accounts/{accountId:guid}/unfreeze")]
    public async Task<ActionResult<AccountStatusResponse>> Unfreeze([FromRoute] Guid accountId, CancellationToken ct)
        => Ok(await mediator.SendAsync(new UnfreezeAccountCommand(accountId), ct));

    /// <summary>Bank report.</summary>
    [HttpGet("reports/bank")]
    public async Task<ActionResult<BankReportResponse>> BankReport(CancellationToken ct)
        => Ok(await mediator.SendAsync(new GetBankReportQuery(), ct));
}

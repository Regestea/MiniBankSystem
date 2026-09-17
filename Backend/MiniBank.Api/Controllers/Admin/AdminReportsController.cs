using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniBank.Features.Messaging;
using MiniBank.Features.Reports.GetBankReport;
using MiniBank.Features.Reports.GetCustomerReport;
using MiniBank.Features.Reports.GetKycReport;
using MiniBank.Features.Reports.GetTransactionReport;

namespace MiniBank.Api.Controllers.Admin;

/// <summary>
/// Admin reports (REST resource: /admin/reports).
/// Every action here requires the "Admin" role — enforced once at controller level.
/// </summary>
[ApiController]
[Route("admin/reports")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin_endpoints")]
[Produces("application/json")]
public sealed class AdminReportsController(IMediator mediator) : ControllerBase
{
    /// <summary>Bank-wide report (customers, accounts, total balance). [Admin]</summary>
    [HttpGet("bank")]
    [ProducesResponseType(typeof(BankReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BankReportResponse>> BankReport(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetBankReportQuery(), cancellationToken));

    /// <summary>Customer report (status breakdown, KYC stats). [Admin]</summary>
    [HttpGet("customers")]
    [ProducesResponseType(typeof(CustomerReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerReportResponse>> CustomerReport(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetCustomerReportQuery(), cancellationToken));

    /// <summary>Transaction report (totals, daily volume). [Admin]</summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(TransactionReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionReportResponse>> TransactionReport([FromQuery] GetTransactionReportQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    /// <summary>KYC report (verification stats). [Admin]</summary>
    [HttpGet("kyc")]
    [ProducesResponseType(typeof(KycReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<KycReportResponse>> KycReport(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetKycReportQuery(), cancellationToken));
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniBank.Features.Accounts;
using MiniBank.Features.Accounts.ApproveAccount;
using MiniBank.Features.Accounts.FreezeAccount;
using MiniBank.Features.Accounts.RejectAccount;
using MiniBank.Features.Accounts.UnfreezeAccount;
using MiniBank.Features.Customers;
using MiniBank.Features.Customers.BlockCustomer;
using MiniBank.Features.Customers.GetCustomer;
using MiniBank.Features.Customers.ListCustomers;
using MiniBank.Features.Customers.VerifyCustomer;
using MiniBank.Features.Messaging;
using MiniBank.Features.Reports.GetBankReport;
using MiniBank.Features.Reports.GetCustomerReport;
using MiniBank.Features.Reports.GetKycReport;
using MiniBank.Features.Reports.GetTransactionReport;

namespace MiniBank.Api.Controllers;

/// <summary>
/// Administrative operations. Every action here requires the "Admin" role —
/// enforced once at controller level.
/// </summary>
[ApiController]
[Route("admin")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin_endpoints")]
[Produces("application/json")]
public sealed class AdminController(IMediator mediator) : ControllerBase
{
    // ── Customers management ────────────────────────────────────────────

    /// <summary>Lists all customers, paged. [Admin]</summary>
    [HttpGet("customers")]
    [ProducesResponseType(typeof(CustomersPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomersPageResponse>> ListCustomers([FromQuery] ListCustomersQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    /// <summary>Returns any customer profile by id. [Admin]</summary>
    [HttpGet("customers/{id:guid}")]
    [ProducesResponseType(typeof(CustomerDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailResponse>> GetCustomer(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetCustomerQuery(id), cancellationToken));

    /// <summary>Verifies a pending customer. [Admin]</summary>
    [HttpPost("customers/{id:guid}/verify")]
    [ProducesResponseType(typeof(VerifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VerifyResponse>> VerifyCustomer(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new VerifyCustomerCommand(id), cancellationToken));

    /// <summary>Blocks a customer. [Admin]</summary>
    [HttpPost("customers/{id:guid}/block")]
    [ProducesResponseType(typeof(BlockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BlockResponse>> BlockCustomer(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new BlockCustomerCommand(id), cancellationToken));

    // ── Accounts management ─────────────────────────────────────────────

    /// <summary>Approves a pending account. [Admin]</summary>
    [HttpPost("accounts/{id:guid}/approve")]
    [ProducesResponseType(typeof(ApproveAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApproveAccountResponse>> ApproveAccount(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new ApproveAccountCommand(id), cancellationToken));

    /// <summary>Rejects a pending account. [Admin]</summary>
    [HttpPost("accounts/{id:guid}/reject")]
    [ProducesResponseType(typeof(RejectAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RejectAccountResponse>> RejectAccount(Guid id, [FromBody] RejectAccountRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new RejectAccountCommand(id, request.Reason), cancellationToken));

    /// <summary>Freezes an account. [Admin]</summary>
    [HttpPost("accounts/{id:guid}/freeze")]
    [ProducesResponseType(typeof(AccountStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountStatusResponse>> FreezeAccount(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new FreezeAccountCommand(id), cancellationToken));

    /// <summary>Unfreezes a frozen account. [Admin]</summary>
    [HttpPost("accounts/{id:guid}/unfreeze")]
    [ProducesResponseType(typeof(AccountStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountStatusResponse>> UnfreezeAccount(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new UnfreezeAccountCommand(id), cancellationToken));

    // ── Reports ─────────────────────────────────────────────────────────

    /// <summary>Bank-wide report (customers, accounts, total balance). [Admin]</summary>
    [HttpGet("reports/bank")]
    [ProducesResponseType(typeof(BankReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BankReportResponse>> BankReport(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetBankReportQuery(), cancellationToken));

    /// <summary>Customer report (status breakdown, KYC stats). [Admin]</summary>
    [HttpGet("reports/customers")]
    [ProducesResponseType(typeof(CustomerReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerReportResponse>> CustomerReport(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetCustomerReportQuery(), cancellationToken));

    /// <summary>Transaction report (totals, daily volume). [Admin]</summary>
    [HttpGet("reports/transactions")]
    [ProducesResponseType(typeof(TransactionReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionReportResponse>> TransactionReport([FromQuery] GetTransactionReportQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    /// <summary>KYC report (verification stats). [Admin]</summary>
    [HttpGet("reports/kyc")]
    [ProducesResponseType(typeof(KycReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<KycReportResponse>> KycReport(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetKycReportQuery(), cancellationToken));
}

public sealed record RejectAccountRequest(string Reason);

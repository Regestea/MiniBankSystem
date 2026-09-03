using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Accounts;
using MiniBank.Features.Accounts.FreezeAccount;
using MiniBank.Features.Accounts.UnfreezeAccount;
using MiniBank.Features.Customers;
using MiniBank.Features.Customers.BlockCustomer;
using MiniBank.Features.Customers.GetCustomer;
using MiniBank.Features.Customers.ListCustomers;
using MiniBank.Features.Customers.VerifyCustomer;
using MiniBank.Features.Messaging;
using MiniBank.Features.Reports.GetBankReport;

namespace MiniBank.Api.Controllers;

/// <summary>
/// Administrative operations. Every action here requires the "Admin" role —
/// enforced once at controller level.
/// </summary>
[ApiController]
[Route("admin")]
[Authorize(Roles = "Admin")]
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
}

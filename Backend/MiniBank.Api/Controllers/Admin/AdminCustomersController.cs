using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniBank.Features.Customers;
using MiniBank.Features.Customers.BlockCustomer;
using MiniBank.Features.Customers.GetCustomer;
using MiniBank.Features.Customers.ListCustomers;
using MiniBank.Features.Customers.UpdateCustomer;
using MiniBank.Features.Customers.VerifyCustomer;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers.Admin;

/// <summary>
/// Admin customer management (REST resource: /admin/customers).
/// Every action here requires the "Admin" role — enforced once at controller level.
/// </summary>
[ApiController]
[Route("admin/customers")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin_endpoints")]
[Produces("application/json")]
public sealed class AdminCustomersController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists all customers, paged. [Admin]</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CustomersPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomersPageResponse>> ListCustomers([FromQuery] ListCustomersQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    /// <summary>Returns any customer profile by id. [Admin]</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailResponse>> GetCustomer(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetCustomerQuery(id), cancellationToken));

    /// <summary>Updates any customer profile. [Admin]</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> UpdateCustomer(Guid id, AdminUpdateCustomerRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new UpdateCustomerCommand(id, request.FullName, request.PhoneNumber), cancellationToken));

    /// <summary>Verifies a pending customer. [Admin]</summary>
    [HttpPost("{id:guid}/verify")]
    [ProducesResponseType(typeof(VerifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VerifyResponse>> VerifyCustomer(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new VerifyCustomerCommand(id), cancellationToken));

    /// <summary>Blocks a customer. [Admin]</summary>
    [HttpPost("{id:guid}/block")]
    [ProducesResponseType(typeof(BlockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BlockResponse>> BlockCustomer(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new BlockCustomerCommand(id), cancellationToken));
}

public sealed record AdminUpdateCustomerRequest(string FullName, string PhoneNumber);

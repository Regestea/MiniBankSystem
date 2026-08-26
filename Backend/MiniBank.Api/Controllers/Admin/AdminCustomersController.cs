using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Customers.BlockCustomer;
using MiniBank.Features.Customers.GetCustomer;
using MiniBank.Features.Customers.ListCustomers;
using MiniBank.Features.Customers.VerifyCustomer;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/customers")]
public sealed class AdminCustomersController(IMediator mediator) : ControllerBase
{
    /// <summary>Admin — paged customer list.</summary>
    [HttpGet]
    public async Task<ActionResult<CustomersPageResponse>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new ListCustomersQuery(page, pageSize), ct));

    /// <summary>Admin — verify a Pending customer (enables account opening).</summary>
    [HttpPatch("{id:guid}/verify")]
    public async Task<IActionResult> Verify([FromRoute] Guid id, CancellationToken ct)
    {
        var response = await mediator.Send(new VerifyCustomerCommand(id), ct);
        return Ok(response);
    }

    /// <summary>Admin — block a customer.</summary>
    [HttpPatch("{id:guid}/block")]
    public async Task<IActionResult> Block([FromRoute] Guid id, CancellationToken ct)
    {
        var response = await mediator.Send(new BlockCustomerCommand(id), ct);
        return Ok(response);
    }
}

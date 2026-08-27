using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Abstractions;
using MiniBank.Features.Customers;
using MiniBank.Features.Customers.CreateCustomer;
using MiniBank.Features.Customers.GetCurrentCustomer;
using MiniBank.Features.Customers.GetCustomer;
using MiniBank.Features.Customers.LinkCustomer;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(IMediator mediator, ICurrentUserContext currentUser) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(
        [FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var command = new CreateCustomerCommand(request.FullName, request.Email, request.PhoneNumber);
        var response = await mediator.SendAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = response.CustomerId }, response);
    }

    [Authorize]
    [HttpPost("me")]
    public async Task<ActionResult<CustomerResponse>> CreateForCurrentUser(
        [FromBody] LinkCustomerRequest request, CancellationToken ct)
    {
        var command = new LinkCustomerCommand(currentUser.UserId, request.FullName, request.PhoneNumber);
        var response = await mediator.SendAsync(command, ct);
        return CreatedAtAction(nameof(GetCurrent), response);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<CustomerDetailResponse>> GetCurrent(CancellationToken ct)
    {
        var customerId = await currentUser.GetCustomerIdAsync(ct);
        if (customerId is null)
            return NotFound(new { errorCode = "no_customer", message = "User has no linked customer profile." });

        var customer = await mediator.SendAsync(new GetCurrentCustomerQuery(currentUser.UserId), ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDetailResponse>> GetById([FromRoute] Guid id, CancellationToken ct)
        => Ok(await mediator.SendAsync(new GetCustomerQuery(id), ct));

    public sealed record CreateCustomerRequest(string FullName, string Email, string PhoneNumber);
    public sealed record LinkCustomerRequest(string FullName, string PhoneNumber);
}

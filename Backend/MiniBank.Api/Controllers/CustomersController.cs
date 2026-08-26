using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Abstractions;
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
    /// <summary>Public sign-up: registers a new customer in Pending state. No auth required — admin will verify.</summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(
        [FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var command = new CreateCustomerCommand(request.FullName, request.Email, request.PhoneNumber);
        var response = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = response.CustomerId }, response);
    }

    /// <summary>Creates the caller's own customer profile (Pending) and links it to the AppUser. Any authenticated user.</summary>
    [Authorize]
    [HttpPost("me")]
    public async Task<ActionResult<CustomerResponse>> CreateForCurrentUser(
        [FromBody] LinkCustomerRequest request, CancellationToken ct)
    {
        var command = new LinkCustomerCommand(currentUser.UserId, request.FullName, request.PhoneNumber);
        var response = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetCurrent), response);
    }

    /// <summary>Returns the caller's own customer profile. GET api/customers (without id) → self. Any authenticated user.</summary>
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<CustomerDetailResponse>> GetCurrent(CancellationToken ct)
    {
        var customerId = await currentUser.GetCustomerIdAsync(ct);
        if (customerId is null)
            return NotFound(new { errorCode = "no_customer", message = "User has no linked customer profile." });

        var customer = await mediator.Send(new GetCurrentCustomerQuery(currentUser.UserId), ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    /// <summary>Admin only: returns any customer by id. GET api/customers/{id} → requires Admin.</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDetailResponse>> GetById([FromRoute] Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetCustomerQuery(id), ct));

    public sealed record CreateCustomerRequest(string FullName, string Email, string PhoneNumber);
    public sealed record LinkCustomerRequest(string FullName, string PhoneNumber);
}

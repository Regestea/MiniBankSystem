using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Abstractions;
using MiniBank.Features.Customers;
using MiniBank.Features.Customers.GetCurrentCustomer;
using MiniBank.Features.Customers.GetCustomer;
using MiniBank.Features.Customers.RegisterCustomer;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(IMediator mediator, ICurrentUserContext currentUser) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<CustomerResponse>> Register(
        [FromBody] RegisterCustomerRequest request, CancellationToken ct)
    {
        var command = new RegisterCustomerCommand(request.Email, request.Password, request.FullName, request.PhoneNumber);
        var response = await mediator.SendAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = response.CustomerId }, response);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<CustomerDetailResponse>> GetCurrent(CancellationToken ct)
    {
        var customer = await mediator.SendAsync(new GetCurrentCustomerQuery(currentUser.UserId), ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDetailResponse>> GetById([FromRoute] Guid id, CancellationToken ct)
        => Ok(await mediator.SendAsync(new GetCustomerQuery(id), ct));

    public sealed record RegisterCustomerRequest(string Email, string Password, string FullName, string PhoneNumber);
}

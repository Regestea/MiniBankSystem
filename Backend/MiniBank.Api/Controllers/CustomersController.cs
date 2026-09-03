using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Customers;
using MiniBank.Features.Customers.GetCurrentCustomer;
using MiniBank.Features.Customers.GetCustomer;
using MiniBank.Features.Customers.RegisterCustomer;
using MiniBank.Features.Customers.UpdateCustomer;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers;

/// <summary>
/// Customer profile endpoints (REST resource: /customers).
/// GET (collection)  → own profile from token.
/// POST (collection) → register (anonymous).
/// GET/PUT by id     → self-service with ownership enforced in handler; admins read any.
/// </summary>
[ApiController]
[Route("customers")]
[Produces("application/json")]
public sealed class CustomersController(IMediator mediator) : ControllerBase
{
    /// <summary>Registers a new customer — atomically creates IdentityUser + Customer profile. (Anonymous)</summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponse>> Register(RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.CustomerId }, response);
    }

    /// <summary>Returns the authenticated caller's own profile (identity from token).</summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(CustomerDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCurrentCustomerQuery(), cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    /// <summary>Returns a customer profile. Owners read their own; admins read any.</summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(CustomerDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetCustomerQuery(id), cancellationToken));

    /// <summary>Updates a customer profile. Only the owner can update (self-service).</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> Update(Guid id, UpdateProfileRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new UpdateCustomerCommand(id, request.FullName, request.PhoneNumber), cancellationToken));
}

public sealed record UpdateProfileRequest(string FullName, string PhoneNumber);

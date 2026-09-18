using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniBank.Features.Customers;
using MiniBank.Features.Customers.GetCurrentCustomer;
using MiniBank.Features.Customers.GetCustomer;
using MiniBank.Features.Customers.GetCustomerOverview;
using MiniBank.Features.Customers.RegisterCustomer;
using MiniBank.Features.Customers.UpdateCurrentCustomer;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers.Customer;

/// <summary>
/// Customer self-service profile (REST resource: /customers).
/// All identity comes from the token — no customer id in any route or body,
/// so one customer can never address another customer's profile.
/// Registration lives here too (POST /customers/register), so auth + profile
/// are one cohesive customer surface instead of two controllers.
/// Amounts across the API are USD.
/// </summary>
[ApiController]
[Route("customers")]
[Produces("application/json")]
public sealed class CustomersController(IMediator mediator) : ControllerBase
{
    /// <summary>Registers a new customer — two-phase (IdentityUser, then Customer profile with compensation). (Anonymous)</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth_endpoints")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponse>> Register(RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProfile), response);
    }

    /// <summary>Returns the authenticated caller's own profile (identity from token).</summary>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(CustomerDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailResponse>> GetProfile(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCurrentCustomerQuery(), cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    /// <summary>Single-page overview: own identity (full name, email, phone) + every account number with balance + total balance.</summary>
    [HttpGet("overview")]
    [Authorize]
    [ProducesResponseType(typeof(CustomerOverviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerOverviewResponse>> GetOverview(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetCustomerOverviewQuery(), cancellationToken));

    /// <summary>Updates the authenticated caller's own profile (identity from token).</summary>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new UpdateCurrentCustomerCommand(request.FullName, request.PhoneNumber), cancellationToken));
}

public sealed record UpdateProfileRequest(string FullName, string PhoneNumber);

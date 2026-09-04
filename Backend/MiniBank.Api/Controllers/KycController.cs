using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Abstractions;
using MiniBank.Features.Kyc.GetKycStatus;
using MiniBank.Features.Kyc.ReviewKyc;
using MiniBank.Features.Kyc.SubmitKyc;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers;

/// <summary>
/// KYC verification operations.
/// </summary>
[ApiController]
[Route("kyc")]
[Authorize]
[Produces("application/json")]
public sealed class KycController(IMediator mediator, ICurrentUserContext currentUser) : ControllerBase
{
    /// <summary>Submits KYC verification. [Authenticated]</summary>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(SubmitKycResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<SubmitKycResponse>> Submit(SubmitKycCommand command, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(command, cancellationToken);
        // NOTE: command.CustomerId is ignored by the handler (forced to caller).
        // Use the authenticated user for the Location header, not client input.
        return CreatedAtAction(nameof(GetStatus), new { customerId = currentUser.UserId }, response);
    }

    /// <summary>Gets KYC status for a customer. [Authenticated]</summary>
    [HttpGet("{customerId:guid}")]
    [ProducesResponseType(typeof(GetKycStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetKycStatusResponse>> GetStatus(Guid customerId, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetKycStatusQuery(customerId), cancellationToken));
}

/// <summary>
/// Admin KYC operations.
/// </summary>
[ApiController]
[Route("admin/kyc")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class AdminKycController(IMediator mediator) : ControllerBase
{
    /// <summary>Reviews KYC verification (approve/reject). [Admin]</summary>
    [HttpPost("{id:guid}/review")]
    [ProducesResponseType(typeof(ReviewKycResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewKycResponse>> Review(Guid id, [FromBody] ReviewKycRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new ReviewKycCommand(id, request.Approve, request.Reason), cancellationToken));
}

public sealed record ReviewKycRequest(bool Approve, string? Reason);

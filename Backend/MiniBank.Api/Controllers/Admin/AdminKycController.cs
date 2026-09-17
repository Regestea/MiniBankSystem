using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Kyc.ReviewKyc;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers.Admin;

/// <summary>Admin KYC operations (REST resource: /admin/kyc).</summary>
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

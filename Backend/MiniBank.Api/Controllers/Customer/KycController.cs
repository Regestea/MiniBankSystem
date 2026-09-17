using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Kyc.GetCurrentKycStatus;
using MiniBank.Features.Kyc.SubmitCurrentKyc;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers.Customer;

/// <summary>
/// Customer KYC self-service (REST resource: /kyc).
/// Identity comes from the token — no customer id in any route or body.
/// Admin review lives in Controllers/Admin/AdminKycController.
/// </summary>
[ApiController]
[Route("kyc")]
[Authorize]
[Produces("application/json")]
public sealed class KycController(IMediator mediator) : ControllerBase
{
    /// <summary>Submits KYC verification for the caller. Body takes only the document id.</summary>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(SubmitCurrentKycResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<SubmitCurrentKycResponse>> Submit(SubmitCurrentKycRequest request, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new SubmitCurrentKycCommand(request.PrimaryDocumentId), cancellationToken);
        return Created("/kyc/status", response);
    }

    /// <summary>Gets the caller's own KYC status (identity from token).</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(GetCurrentKycStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetCurrentKycStatusResponse>> GetStatus(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetCurrentKycStatusQuery(), cancellationToken));
}

public sealed record SubmitCurrentKycRequest(Guid PrimaryDocumentId);

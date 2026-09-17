using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Risk.GetCustomerRisk;
using MiniBank.Features.Risk.GetCurrentCustomerRisk;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers.Customer;

/// <summary>
/// Customer risk self-service (REST resource: /risk).
/// Identity comes from the token — no customer id in any route.
/// Admin management lives in Controllers/Admin/AdminRiskController.
/// </summary>
[ApiController]
[Route("risk")]
[Authorize]
[Produces("application/json")]
public sealed class RiskController(IMediator mediator) : ControllerBase
{
    /// <summary>Gets the caller's own risk info (identity from token).</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(GetCustomerRiskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCustomerRiskResponse>> GetStatus(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetCurrentCustomerRiskQuery(), cancellationToken));
}

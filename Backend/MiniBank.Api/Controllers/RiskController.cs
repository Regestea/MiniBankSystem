using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Risk.GetCustomerRisk;
using MiniBank.Features.Risk.ListHighRiskCustomers;
using MiniBank.Features.Risk.UpdateRiskLevel;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers;

/// <summary>
/// Risk management operations.
/// </summary>
[ApiController]
[Route("risk")]
[Authorize]
[Produces("application/json")]
public sealed class RiskController(IMediator mediator) : ControllerBase
{
    /// <summary>Gets customer risk info. [Authenticated]</summary>
    [HttpGet("{customerId:guid}")]
    [ProducesResponseType(typeof(GetCustomerRiskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCustomerRiskResponse>> GetRisk(Guid customerId, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetCustomerRiskQuery(customerId), cancellationToken));
}

/// <summary>
/// Admin risk management operations.
/// </summary>
[ApiController]
[Route("admin/risk")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class AdminRiskController(IMediator mediator) : ControllerBase
{
    /// <summary>Updates customer risk level. [Admin]</summary>
    [HttpPost("{customerId:guid}/level")]
    [ProducesResponseType(typeof(UpdateRiskLevelResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpdateRiskLevelResponse>> UpdateLevel(Guid customerId, [FromBody] UpdateRiskLevelRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new UpdateRiskLevelCommand(customerId, request.RiskLevel), cancellationToken));

    /// <summary>Lists high risk customers. [Admin]</summary>
    [HttpGet("high-risk")]
    [ProducesResponseType(typeof(ListHighRiskCustomersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListHighRiskCustomersResponse>> ListHighRisk([FromQuery] ListHighRiskCustomersQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));
}

public sealed record UpdateRiskLevelRequest(string RiskLevel);

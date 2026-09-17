using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Audit.GetAuditLogs;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers.Admin;

/// <summary>Admin audit operations (REST resource: /admin/audit).</summary>
[ApiController]
[Route("admin/audit")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class AdminAuditController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists audit logs with filters. [Admin]</summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(GetAuditLogsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetAuditLogsResponse>> GetLogs([FromQuery] GetAuditLogsQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));
}

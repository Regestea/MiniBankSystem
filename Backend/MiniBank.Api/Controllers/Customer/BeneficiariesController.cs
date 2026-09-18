using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Beneficiaries.AddBeneficiary;
using MiniBank.Features.Beneficiaries.ListBeneficiaries;
using MiniBank.Features.Beneficiaries.RemoveBeneficiary;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers.Customer;

/// <summary>
/// Saved beneficiaries (REST resource: /beneficiaries).
/// When the caller ticks "save" during a transfer, the destination account number
/// + holder name is remembered here for next transfers.
/// </summary>
[ApiController]
[Route("beneficiaries")]
[Authorize]
[Produces("application/json")]
public sealed class BeneficiariesController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists the caller's saved transfer destinations (account number + holder name).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BeneficiaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BeneficiaryDto>>> List(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new ListBeneficiariesQuery(), cancellationToken));

    /// <summary>Saves a destination by account number (IR-XXXXXXXXXX or 16-digit). Holder name is resolved server-side; optional nickname overrides the display name.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BeneficiaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BeneficiaryDto>> Add(AddBeneficiaryCommand command, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(command, cancellationToken);
        return Created($"/beneficiaries/{response.BeneficiaryId}", response);
    }

    /// <summary>Removes one of the caller's saved destinations.</summary>
    [HttpDelete("{beneficiaryId:guid}")]
    [ProducesResponseType(typeof(RemoveBeneficiaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RemoveBeneficiaryResponse>> Remove(Guid beneficiaryId, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new RemoveBeneficiaryCommand(beneficiaryId), cancellationToken));
}

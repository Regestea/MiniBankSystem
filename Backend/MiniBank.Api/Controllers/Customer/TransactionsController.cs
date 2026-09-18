using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniBank.Features.Messaging;
using MiniBank.Features.Transactions.ListMyTransactions;

namespace MiniBank.Api.Controllers.Customer;

/// <summary>Customer transaction history (REST resource: /transactions). Scoped to the caller's own accounts. Amounts are USD.</summary>
[ApiController]
[Route("transactions")]
[Authorize]
[Produces("application/json")]
public sealed class TransactionsController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists the caller's transactions across all own accounts (newest first, paged) with running account numbers and balances via /accounts.</summary>
    [HttpGet("mine")]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(MyTransactionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MyTransactionsResponse>> ListMine(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(new ListMyTransactionsQuery(page, pageSize), cancellationToken));
}

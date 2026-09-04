using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiniBank.Features.Accounts;
using MiniBank.Features.Accounts.CloseAccount;
using MiniBank.Features.Accounts.Deposit;
using MiniBank.Features.Accounts.GetAccounts;
using MiniBank.Features.Accounts.GetStatement;
using MiniBank.Features.Accounts.OpenAccount;
using MiniBank.Features.Accounts.Withdraw;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers;

[ApiController]
[Route("accounts")]
[Authorize]
public sealed class AccountsController(IMediator mediator) : ControllerBase
{
    /// <summary>Opens a new account for the authenticated customer.</summary>
    [HttpPost]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AccountResponse>> Open(OpenAccountCommand command, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(command, cancellationToken);
        return Created($"/accounts/{response.AccountId}/statement", response);
    }

    /// <summary>Lists the authenticated customer's accounts with balances (paged).</summary>
    [HttpGet]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(IReadOnlyList<AccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAccounts([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(new GetAccountsQuery(page, pageSize), cancellationToken));

    /// <summary>Returns a paged statement (ordered ledger entries) for one of the caller's accounts.</summary>
    [HttpGet("{accountId:guid}/statement")]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(StatementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StatementResponse>> GetStatement(
        Guid accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(new GetStatementQuery(accountId, page, pageSize), cancellationToken));

    /// <summary>Deposits money into one of the caller's accounts. Idempotent: same key+payload returns the original transaction (200); same key+different payload is 409.</summary>
    [HttpPost("{accountId:guid}/deposit")]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransactionResponse>> Deposit(Guid accountId, DepositRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new DepositCommand(accountId, request.Amount, request.IdempotencyKey), cancellationToken));

    /// <summary>Withdraws money from one of the caller's accounts.</summary>
    [HttpPost("{accountId:guid}/withdraw")]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransactionResponse>> Withdraw(Guid accountId, WithdrawRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new WithdrawCommand(accountId, request.Amount, request.IdempotencyKey), cancellationToken));

    /// <summary>Closes an account (balance must be zero).</summary>
    [HttpPost("{accountId:guid}/close")]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(CloseAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CloseAccountResponse>> Close(Guid accountId, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new CloseAccountCommand(accountId), cancellationToken));
}

public sealed record DepositRequest(decimal Amount, string IdempotencyKey);
public sealed record WithdrawRequest(decimal Amount, string IdempotencyKey);

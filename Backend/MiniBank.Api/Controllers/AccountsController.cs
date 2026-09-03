using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AccountResponse>> Open(OpenAccountCommand command, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Lists the authenticated customer's accounts with balances.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAccounts(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetAccountsQuery(), cancellationToken));

    /// <summary>Returns a paged statement (ordered ledger entries) for one of the caller's accounts.</summary>
    [HttpGet("{accountId:guid}/statement")]
    [ProducesResponseType(typeof(StatementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StatementResponse>> GetStatement(
        Guid accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(new GetStatementQuery(accountId, page, pageSize), cancellationToken));

    /// <summary>Deposits money into one of the caller's accounts.</summary>
    [HttpPost("{accountId:guid}/deposit")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TransactionResponse>> Deposit(Guid accountId, DepositRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new DepositCommand(accountId, request.Amount), cancellationToken));

    /// <summary>Withdraws money from one of the caller's accounts.</summary>
    [HttpPost("{accountId:guid}/withdraw")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransactionResponse>> Withdraw(Guid accountId, WithdrawRequest request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new WithdrawCommand(accountId, request.Amount), cancellationToken));

    /// <summary>Closes an account (balance must be zero).</summary>
    [HttpPost("{accountId:guid}/close")]
    [ProducesResponseType(typeof(CloseAccountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CloseAccountResponse>> Close(Guid accountId, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new CloseAccountCommand(accountId), cancellationToken));
}

public sealed record DepositRequest(decimal Amount);
public sealed record WithdrawRequest(decimal Amount);

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniBank.Features.Abstractions;
using MiniBank.Features.Accounts;
using MiniBank.Features.Accounts.CloseAccount;
using MiniBank.Features.Accounts.Deposit;
using MiniBank.Features.Accounts.GetAccounts;
using MiniBank.Features.Accounts.GetStatement;
using MiniBank.Features.Accounts.OpenAccount;
using MiniBank.Features.Accounts.Transfer;
using MiniBank.Features.Accounts.Withdraw;
using MiniBank.Features.Messaging;

namespace MiniBank.Api.Controllers;

[ApiController]
[Authorize(Roles = "User,Admin")]
[Route("api/accounts")]
public sealed class AccountsController(IMediator mediator, ICurrentUserContext currentUser) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AccountResponse>> Open(
        [FromBody] OpenAccountRequest request, CancellationToken ct)
    {
        var command = new OpenAccountCommand(currentUser.UserId, request.AccountType);
        var response = await mediator.SendAsync(command, ct);
        return CreatedAtAction(nameof(GetStatement), new { accountId = response.AccountId }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> List(CancellationToken ct)
        => Ok(await mediator.SendAsync(new GetAccountsQuery(currentUser.UserId), ct));

    [HttpPost("{accountId:guid}/deposit")]
    public async Task<ActionResult<TransactionResponse>> Deposit(
        [FromRoute] Guid accountId, [FromBody] AmountRequest request, CancellationToken ct)
    {
        var response = await mediator.SendAsync(new DepositCommand(accountId, request.Amount), ct);
        return Ok(response);
    }

    [HttpPost("{accountId:guid}/withdraw")]
    public async Task<ActionResult<TransactionResponse>> Withdraw(
        [FromRoute] Guid accountId, [FromBody] AmountRequest request, CancellationToken ct)
    {
        var response = await mediator.SendAsync(new WithdrawCommand(accountId, request.Amount), ct);
        return Ok(response);
    }

    [HttpPost("transfer")]
    public async Task<ActionResult<TransferResponse>> Transfer(
        [FromBody] TransferRequest request, CancellationToken ct)
    {
        var response = await mediator.SendAsync(
            new TransferCommand(request.FromAccountId, request.ToAccountId, request.Amount), ct);
        return Ok(response);
    }

    [HttpGet("{accountId:guid}/statement")]
    public async Task<ActionResult<StatementResponse>> GetStatement(
        [FromRoute] Guid accountId, CancellationToken ct)
        => Ok(await mediator.SendAsync(new GetStatementQuery(accountId, currentUser.UserId), ct));

    [HttpPatch("{accountId:guid}/close")]
    public async Task<IActionResult> Close([FromRoute] Guid accountId, CancellationToken ct)
    {
        var response = await mediator.SendAsync(new CloseAccountCommand(accountId), ct);
        return Ok(response);
    }

    public sealed record OpenAccountRequest(string AccountType);
    public sealed record AmountRequest(decimal Amount);
    public sealed record TransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount);
}

using System.Net;
using System.Net.Http.Json;
using FluentValidation;
using FluentValidation.Results;
using MiniBank.Features.Accounts;
using MiniBank.Features.Accounts.Deposit;
using MiniBank.Features.Accounts.GetAccounts;
using MiniBank.Features.Accounts.GetStatement;
using MiniBank.Features.Accounts.OpenAccount;
using MiniBank.Features.Accounts.Transfer;
using MiniBank.Features.Accounts.Withdraw;

namespace MiniBank.Api.Tests;

[Collection("Sequential")]
public class AccountsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _unauthClient;
    private readonly HttpClient _authClient;
    private readonly TestWebApplicationFactory _factory;

    private static readonly ValidationException ValidationEx =
        new([new ValidationFailure("field", "invalid")]);

    public AccountsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMock();
        _unauthClient = factory.CreateClient();
        _authClient = factory.CreateAuthenticatedClient();
    }

    private static Task<T> ThrowValidation<T>() => throw ValidationEx;

    // --- 401 Unauthorized tests ---

    [Fact]
    public async Task Open_Returns401_WhenNotAuthenticated()
    {
        var command = new { CurrencyCode = "USD" };
        var response = await _unauthClient.PostAsJsonAsync("/accounts", command);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccounts_Returns401_WhenNotAuthenticated()
    {
        var response = await _unauthClient.GetAsync("/accounts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStatement_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var response = await _unauthClient.GetAsync($"/accounts/{id}/statement");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deposit_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var request = new { Amount = 100m, IdempotencyKey = Guid.NewGuid().ToString() };
        var response = await _unauthClient.PostAsJsonAsync($"/accounts/{id}/deposit", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Withdraw_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var request = new { Amount = 50m, IdempotencyKey = Guid.NewGuid().ToString() };
        var response = await _unauthClient.PostAsJsonAsync($"/accounts/{id}/withdraw", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Close_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var response = await _unauthClient.PostAsJsonAsync($"/accounts/{id}/close", (object?)null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- 400 Bad Request tests ---

    [Fact]
    public async Task Open_Returns400_WhenAccountTypeIsInvalid()
    {
        _factory.MockMediator.Send(Arg.Any<OpenAccountCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AccountResponse>(ValidationEx));

        var command = new { AccountType = "INVALID" };
        var response = await _authClient.PostAsJsonAsync("/accounts", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deposit_Returns400_WhenAmountIsZero()
    {
        _factory.MockMediator.Send(Arg.Any<DepositCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TransactionResponse>(ValidationEx));

        var id = Guid.NewGuid();
        var request = new { Amount = 0m, IdempotencyKey = Guid.NewGuid().ToString() };
        var response = await _authClient.PostAsJsonAsync($"/accounts/{id}/deposit", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deposit_Returns400_WhenAmountIsNegative()
    {
        _factory.MockMediator.Send(Arg.Any<DepositCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TransactionResponse>(ValidationEx));

        var id = Guid.NewGuid();
        var request = new { Amount = -100m, IdempotencyKey = Guid.NewGuid().ToString() };
        var response = await _authClient.PostAsJsonAsync($"/accounts/{id}/deposit", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Withdraw_Returns400_WhenAmountIsZero()
    {
        _factory.MockMediator.Send(Arg.Any<WithdrawCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TransactionResponse>(ValidationEx));

        var id = Guid.NewGuid();
        var request = new { Amount = 0m, IdempotencyKey = Guid.NewGuid().ToString() };
        var response = await _authClient.PostAsJsonAsync($"/accounts/{id}/withdraw", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Withdraw_Returns400_WhenAmountIsNegative()
    {
        _factory.MockMediator.Send(Arg.Any<WithdrawCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TransactionResponse>(ValidationEx));

        var id = Guid.NewGuid();
        var request = new { Amount = -50m, IdempotencyKey = Guid.NewGuid().ToString() };
        var response = await _authClient.PostAsJsonAsync($"/accounts/{id}/withdraw", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deposit_Returns400_WhenIdempotencyKeyIsEmpty()
    {
        _factory.MockMediator.Send(Arg.Any<DepositCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TransactionResponse>(ValidationEx));

        var id = Guid.NewGuid();
        var request = new { Amount = 100m, IdempotencyKey = "" };
        var response = await _authClient.PostAsJsonAsync($"/accounts/{id}/deposit", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- 404 Not Found tests ---

    [Fact]
    public async Task GetStatement_Returns404_WhenAccountNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        _factory.MockMediator.Send(Arg.Any<GetStatementQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StatementResponse>(
                new Domain.BuildingBlocks.Exceptions.NotFoundException("account", nonExistentId)));

        var response = await _authClient.GetAsync($"/accounts/{nonExistentId}/statement");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Happy path tests ---

    [Fact]
    public async Task Open_Returns201_WhenValidCommand()
    {
        var accountId = Guid.NewGuid();
        _factory.MockMediator.Send(Arg.Any<OpenAccountCommand>(), Arg.Any<CancellationToken>())
            .Returns(new AccountResponse(accountId, "1234567890", "Savings", "Pending", DateTimeOffset.UtcNow));

        var command = new { AccountType = "Savings" };
        var response = await _authClient.PostAsJsonAsync("/accounts", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(accountId.ToString());

        var body = await response.Content.ReadFromJsonAsync<AccountResponse>();
        body.Should().NotBeNull();
        body!.AccountId.Should().Be(accountId);
    }

    [Fact]
    public async Task GetAccounts_Returns200_WhenAuthenticated()
    {
        _factory.MockMediator.Send(Arg.Any<GetAccountsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<AccountDto>
            {
                new(Guid.NewGuid(), "1234567890", "Savings", "Active", 1000m, DateTimeOffset.UtcNow)
            });

        var response = await _authClient.GetAsync("/accounts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<List<AccountDto>>();
        body.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Deposit_Returns200_WhenValidAmount()
    {
        var accountId = Guid.NewGuid();
        _factory.MockMediator.Send(Arg.Any<DepositCommand>(), Arg.Any<CancellationToken>())
            .Returns(new TransactionResponse(Guid.NewGuid(), "Deposit", 100m, "idem-1", DateTimeOffset.UtcNow));

        var request = new { Amount = 100m, IdempotencyKey = "idem-1" };
        var response = await _authClient.PostAsJsonAsync($"/accounts/{accountId}/deposit", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Route + body must map to the command (not just return 200 for any stub).
        await _factory.MockMediator.Received(1).Send(
            Arg.Is<DepositCommand>(c => c.AccountId == accountId && c.Amount == 100m && c.IdempotencyKey == "idem-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Withdraw_Returns200_WhenValidAmount()
    {
        var accountId = Guid.NewGuid();
        _factory.MockMediator.Send(Arg.Any<WithdrawCommand>(), Arg.Any<CancellationToken>())
            .Returns(new TransactionResponse(Guid.NewGuid(), "Withdrawal", 50m, "idem-2", DateTimeOffset.UtcNow));

        var request = new { Amount = 50m, IdempotencyKey = "idem-2" };
        var response = await _authClient.PostAsJsonAsync($"/accounts/{accountId}/withdraw", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStatement_Returns200_WhenValidAccountId()
    {
        var accountId = Guid.NewGuid();
        _factory.MockMediator.Send(Arg.Any<GetStatementQuery>(), Arg.Any<CancellationToken>())
            .Returns(new StatementResponse(accountId, "1234567890", "Active", 500m, 1, 20, 0, []));

        var response = await _authClient.GetAsync($"/accounts/{accountId}/statement");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

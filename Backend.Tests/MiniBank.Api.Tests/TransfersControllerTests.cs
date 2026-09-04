using System.Net;
using System.Net.Http.Json;
using FluentValidation;
using FluentValidation.Results;
using MiniBank.Features.Accounts;
using MiniBank.Features.Accounts.Transfer;

namespace MiniBank.Api.Tests;

[Collection("Sequential")]
public class TransfersControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _unauthClient;
    private readonly HttpClient _authClient;
    private readonly TestWebApplicationFactory _factory;

    private static readonly ValidationException ValidationEx =
        new([new ValidationFailure("field", "invalid")]);

    public TransfersControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMock();
        _unauthClient = factory.CreateClient();
        _authClient = factory.CreateAuthenticatedClient();
    }

    // --- 401 Unauthorized ---

    [Fact]
    public async Task Transfer_Returns401_WhenNotAuthenticated()
    {
        var command = new
        {
            FromAccountId = Guid.NewGuid(),
            ToAccountId = Guid.NewGuid(),
            Amount = 100m,
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        var response = await _unauthClient.PostAsJsonAsync("/transfers", command);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- 400 Bad Request ---

    [Fact]
    public async Task Transfer_Returns400_WhenAmountIsZero()
    {
        _factory.MockMediator.Send(Arg.Any<TransferCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TransferResponse>(ValidationEx));

        var command = new
        {
            FromAccountId = Guid.NewGuid(),
            ToAccountId = Guid.NewGuid(),
            Amount = 0m,
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        var response = await _authClient.PostAsJsonAsync("/transfers", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Transfer_Returns400_WhenAmountIsNegative()
    {
        _factory.MockMediator.Send(Arg.Any<TransferCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TransferResponse>(ValidationEx));

        var command = new
        {
            FromAccountId = Guid.NewGuid(),
            ToAccountId = Guid.NewGuid(),
            Amount = -100m,
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        var response = await _authClient.PostAsJsonAsync("/transfers", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Transfer_Returns400_WhenSameSourceAndDestination()
    {
        _factory.MockMediator.Send(Arg.Any<TransferCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TransferResponse>(ValidationEx));

        var accountId = Guid.NewGuid();
        var command = new
        {
            FromAccountId = accountId,
            ToAccountId = accountId,
            Amount = 100m,
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        var response = await _authClient.PostAsJsonAsync("/transfers", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Transfer_Returns400_WhenIdempotencyKeyIsEmpty()
    {
        _factory.MockMediator.Send(Arg.Any<TransferCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TransferResponse>(ValidationEx));

        var command = new
        {
            FromAccountId = Guid.NewGuid(),
            ToAccountId = Guid.NewGuid(),
            Amount = 100m,
            IdempotencyKey = ""
        };
        var response = await _authClient.PostAsJsonAsync("/transfers", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- Happy path ---

    [Fact]
    public async Task Transfer_Returns200_WhenValidCommand()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        _factory.MockMediator.Send(Arg.Any<TransferCommand>(), Arg.Any<CancellationToken>())
            .Returns(new TransferResponse(
                Guid.NewGuid(), 100m, "idem-1", fromId, toId, DateTimeOffset.UtcNow));

        var command = new
        {
            FromAccountId = fromId,
            ToAccountId = toId,
            Amount = 100m,
            IdempotencyKey = "idem-1"
        };
        var response = await _authClient.PostAsJsonAsync("/transfers", command);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TransferResponse>();
        body.Should().NotBeNull();
        body!.Amount.Should().Be(100m);
    }
}

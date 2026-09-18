using System.Net;
using System.Net.Http.Json;

namespace MiniBank.Api.Tests.Integration;

/// <summary>
/// End-to-end account journeys: onboarding account, open/list, top-up,
/// idempotent deposit, withdraw, statement and close.
/// </summary>
[Collection("api-integration")]
public sealed class AccountsJourneyTests(ApiIntegrationFixture fixture)
{
    [Fact]
    public async Task Overview_ContainsAutoCreatedActiveAccountWithZeroBalance()
    {
        var session = await fixture.RegisterUserAsync();

        session.Overview!.Accounts.Should().ContainSingle();
        var account = session.Overview.Accounts[0];
        account.Status.Should().Be("Active");
        account.Balance.Should().Be(0);
        session.Overview.TotalBalance.Should().Be(0);
    }

    [Fact]
    public async Task OpenAccount_CreatesPendingAccount()
    {
        var session = await fixture.RegisterUserAsync();

        var response = await session.Client.PostAsJsonAsync("/accounts", new { accountType = "Savings" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var overview = await session.Client.GetFromJsonAsync<OverviewResponse>(
            "/customers/overview", ApiIntegrationFixture.Json);
        overview!.Accounts.Should().HaveCount(2);
        overview.Accounts.Should().Contain(a => a.Status == "PendingApproval" && a.AccountType == "Savings");
    }

    [Fact]
    public async Task OpenAccount_InvalidType_Returns400()
    {
        var session = await fixture.RegisterUserAsync();

        var response = await session.Client.PostAsJsonAsync("/accounts", new { accountType = "Gold" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Topup_IncreasesBalance()
    {
        var session = await fixture.RegisterUserAsync();
        var accountId = session.PrimaryAccount.AccountId;

        var response = await session.Client.PostAsJsonAsync($"/accounts/{accountId}/topup", new { amount = 250m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tx = (await response.Content.ReadFromJsonAsync<TransactionResponseDto>(ApiIntegrationFixture.Json))!;
        tx.Amount.Should().Be(250m);

        var overview = await session.Client.GetFromJsonAsync<OverviewResponse>(
            "/customers/overview", ApiIntegrationFixture.Json);
        overview!.TotalBalance.Should().Be(250m);
    }

    [Fact]
    public async Task Topup_PendingAccount_Returns409()
    {
        var session = await fixture.RegisterUserAsync();
        var opened = await session.Client.PostAsJsonAsync("/accounts", new { accountType = "Current" });
        opened.StatusCode.Should().Be(HttpStatusCode.Created);
        var pending = (await opened.Content.ReadFromJsonAsync<AccountResponseDto>(ApiIntegrationFixture.Json))!;
        pending.Status.Should().Be("PendingApproval");

        var response = await session.Client.PostAsJsonAsync($"/accounts/{pending.AccountId}/topup", new { amount = 10m });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Deposit_IsIdempotent_SameKeyReturnsSameTransaction()
    {
        var session = await fixture.RegisterUserAsync();
        var accountId = session.PrimaryAccount.AccountId;
        var key = Guid.NewGuid().ToString("N");

        var first = await session.Client.PostAsJsonAsync($"/accounts/{accountId}/deposit", new { amount = 100m, idempotencyKey = key });
        var second = await session.Client.PostAsJsonAsync($"/accounts/{accountId}/deposit", new { amount = 100m, idempotencyKey = key });

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstTx = (await first.Content.ReadFromJsonAsync<TransactionResponseDto>(ApiIntegrationFixture.Json))!;
        var secondTx = (await second.Content.ReadFromJsonAsync<TransactionResponseDto>(ApiIntegrationFixture.Json))!;
        secondTx.TransactionId.Should().Be(firstTx.TransactionId);

        var overview = await session.Client.GetFromJsonAsync<OverviewResponse>(
            "/customers/overview", ApiIntegrationFixture.Json);
        overview!.TotalBalance.Should().Be(100m);
    }

    [Fact]
    public async Task Deposit_SameKeyDifferentPayload_Returns409()
    {
        var session = await fixture.RegisterUserAsync();
        var accountId = session.PrimaryAccount.AccountId;
        var key = Guid.NewGuid().ToString("N");

        await session.Client.PostAsJsonAsync($"/accounts/{accountId}/deposit", new { amount = 100m, idempotencyKey = key });
        var conflict = await session.Client.PostAsJsonAsync($"/accounts/{accountId}/deposit", new { amount = 50m, idempotencyKey = key });

        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Withdraw_DecreasesBalance_AndOverdraft_Returns422()
    {
        var session = await fixture.RegisterUserAsync();
        var accountId = session.PrimaryAccount.AccountId;
        await session.Client.PostAsJsonAsync($"/accounts/{accountId}/topup", new { amount = 200m });

        var withdraw = await session.Client.PostAsJsonAsync(
            $"/accounts/{accountId}/withdraw", new { amount = 70m, idempotencyKey = Guid.NewGuid().ToString("N") });
        withdraw.StatusCode.Should().Be(HttpStatusCode.OK);

        var overdraft = await session.Client.PostAsJsonAsync(
            $"/accounts/{accountId}/withdraw", new { amount = 1000m, idempotencyKey = Guid.NewGuid().ToString("N") });
        overdraft.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var overview = await session.Client.GetFromJsonAsync<OverviewResponse>(
            "/customers/overview", ApiIntegrationFixture.Json);
        overview!.TotalBalance.Should().Be(130m);
    }

    [Fact]
    public async Task Statement_ListsEntriesNewestBalanceMatches()
    {
        var session = await fixture.RegisterUserAsync();
        var accountId = session.PrimaryAccount.AccountId;
        await session.Client.PostAsJsonAsync($"/accounts/{accountId}/topup", new { amount = 300m });
        await session.Client.PostAsJsonAsync(
            $"/accounts/{accountId}/withdraw", new { amount = 40m, idempotencyKey = Guid.NewGuid().ToString("N") });

        var response = await session.Client.GetAsync($"/accounts/{accountId}/statement?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var statement = (await response.Content.ReadFromJsonAsync<StatementResponseDto>(ApiIntegrationFixture.Json))!;
        statement.Balance.Should().Be(260m);
        statement.Total.Should().BeGreaterThanOrEqualTo(2);
        statement.Entries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Close_ZeroBalanceAccount_ReturnsClosed_AndBlocksFurtherTopup()
    {
        var session = await fixture.RegisterUserAsync();
        var accountId = session.PrimaryAccount.AccountId;

        var close = await session.Client.PostAsync($"/accounts/{accountId}/close", null);

        close.StatusCode.Should().Be(HttpStatusCode.OK);
        var closed = (await close.Content.ReadFromJsonAsync<AccountStatusDto>(ApiIntegrationFixture.Json))!;
        closed.Status.Should().Be("Closed");

        var topup = await session.Client.PostAsJsonAsync($"/accounts/{accountId}/topup", new { amount = 10m });
        topup.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Close_NonZeroBalanceAccount_Returns422()
    {
        var session = await fixture.RegisterUserAsync();
        var accountId = session.PrimaryAccount.AccountId;
        await session.Client.PostAsJsonAsync($"/accounts/{accountId}/topup", new { amount = 10m });

        var close = await session.Client.PostAsync($"/accounts/{accountId}/close", null);

        close.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetAccounts_Unauthenticated_Returns401()
    {
        var client = fixture.CreateClient();

        (await client.GetAsync("/accounts")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

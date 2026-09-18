using System.Net;
using System.Net.Http.Json;

namespace MiniBank.Api.Tests.Integration;

/// <summary>
/// End-to-end transfer journeys between two real users: preview, transfer by
/// account number / by ids, balances, history and the failure modes.
/// </summary>
[Collection("api-integration")]
public sealed class TransfersJourneyTests(ApiIntegrationFixture fixture)
{
    [Fact]
    public async Task PreviewByNumber_ReturnsHolderFullName()
    {
        var sender = await fixture.RegisterUserAsync(fullName: "Sender User");
        var receiver = await fixture.RegisterUserAsync(fullName: "Receiver User");

        var response = await sender.Client.PostAsJsonAsync("/transfers/preview-by-number", new
        {
            accountNumber = receiver.PrimaryAccount.AccountNumber,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = (await response.Content.ReadFromJsonAsync<PreviewResponseDto>(ApiIntegrationFixture.Json))!;
        preview.HolderFullName.Should().Be("Receiver User");
        preview.AccountNumber.Should().Be(receiver.PrimaryAccount.AccountNumber);
        preview.MaskedHolderName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PreviewByNumber_UnknownAccount_Returns404()
    {
        var sender = await fixture.RegisterUserAsync();

        var response = await sender.Client.PostAsJsonAsync("/transfers/preview-by-number", new
        {
            accountNumber = "IR-9999999999",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PreviewByNumber_InvalidFormat_Returns400()
    {
        var sender = await fixture.RegisterUserAsync();

        var response = await sender.Client.PostAsJsonAsync("/transfers/preview-by-number", new
        {
            accountNumber = "nope",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TransferByNumber_MovesMoney_AndUpdatesBothBalances()
    {
        var sender = await fixture.RegisterUserAsync();
        var receiver = await fixture.RegisterUserAsync();
        await sender.Client.PostAsJsonAsync($"/accounts/{sender.PrimaryAccount.AccountId}/topup", new { amount = 500m });

        var response = await sender.Client.PostAsJsonAsync("/transfers/by-account-number", new
        {
            fromAccountId = sender.PrimaryAccount.AccountId,
            toAccountNumber = receiver.PrimaryAccount.AccountNumber,
            amount = 150m,
            saveBeneficiary = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = (await response.Content.ReadFromJsonAsync<TransferResponseDto>(ApiIntegrationFixture.Json))!;
        transfer.Amount.Should().Be(150m);
        transfer.FromAccountId.Should().Be(sender.PrimaryAccount.AccountId);
        transfer.ToAccountId.Should().Be(receiver.PrimaryAccount.AccountId);

        var senderOverview = await sender.Client.GetFromJsonAsync<OverviewResponse>(
            "/customers/overview", ApiIntegrationFixture.Json);
        var receiverOverview = await receiver.Client.GetFromJsonAsync<OverviewResponse>(
            "/customers/overview", ApiIntegrationFixture.Json);
        senderOverview!.TotalBalance.Should().Be(350m);
        receiverOverview!.TotalBalance.Should().Be(150m);
    }

    [Fact]
    public async Task TransferByNumber_WithSaveBeneficiary_RemembersDestination()
    {
        var sender = await fixture.RegisterUserAsync();
        var receiver = await fixture.RegisterUserAsync();
        await sender.Client.PostAsJsonAsync($"/accounts/{sender.PrimaryAccount.AccountId}/topup", new { amount = 500m });

        var transfer = await sender.Client.PostAsJsonAsync("/transfers/by-account-number", new
        {
            fromAccountId = sender.PrimaryAccount.AccountId,
            toAccountNumber = receiver.PrimaryAccount.AccountNumber,
            amount = 20m,
            saveBeneficiary = true,
        });
        transfer.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await sender.Client.GetFromJsonAsync<List<BeneficiaryDto>>(
            "/beneficiaries", ApiIntegrationFixture.Json);
        list.Should().ContainSingle(b => b.AccountNumber == receiver.PrimaryAccount.AccountNumber);
    }

    [Fact]
    public async Task TransferByIds_MovesMoney()
    {
        var sender = await fixture.RegisterUserAsync();
        var receiver = await fixture.RegisterUserAsync();
        await sender.Client.PostAsJsonAsync($"/accounts/{sender.PrimaryAccount.AccountId}/topup", new { amount = 400m });

        var response = await sender.Client.PostAsJsonAsync("/transfers", new
        {
            fromAccountId = sender.PrimaryAccount.AccountId,
            toAccountId = receiver.PrimaryAccount.AccountId,
            amount = 100m,
            idempotencyKey = Guid.NewGuid().ToString("N"),
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var senderOverview = await sender.Client.GetFromJsonAsync<OverviewResponse>(
            "/customers/overview", ApiIntegrationFixture.Json);
        senderOverview!.TotalBalance.Should().Be(300m);
    }

    [Fact]
    public async Task Transfer_InsufficientFunds_Returns422()
    {
        var sender = await fixture.RegisterUserAsync();
        var receiver = await fixture.RegisterUserAsync();

        var response = await sender.Client.PostAsJsonAsync("/transfers/by-account-number", new
        {
            fromAccountId = sender.PrimaryAccount.AccountId,
            toAccountNumber = receiver.PrimaryAccount.AccountNumber,
            amount = 10000m,
            saveBeneficiary = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Transfer_ToSelf_Returns400()
    {
        var sender = await fixture.RegisterUserAsync();
        await sender.Client.PostAsJsonAsync($"/accounts/{sender.PrimaryAccount.AccountId}/topup", new { amount = 100m });

        var response = await sender.Client.PostAsJsonAsync("/transfers/by-account-number", new
        {
            fromAccountId = sender.PrimaryAccount.AccountId,
            toAccountNumber = sender.PrimaryAccount.AccountNumber,
            amount = 10m,
            saveBeneficiary = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Transfer_FromAnotherUsersAccount_Returns403()
    {
        var sender = await fixture.RegisterUserAsync();
        var receiver = await fixture.RegisterUserAsync();

        var response = await sender.Client.PostAsJsonAsync("/transfers/by-account-number", new
        {
            fromAccountId = receiver.PrimaryAccount.AccountId,
            toAccountNumber = sender.PrimaryAccount.AccountNumber,
            amount = 10m,
            saveBeneficiary = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MyTransactions_ContainsTransferNewestFirst()
    {
        var sender = await fixture.RegisterUserAsync();
        var receiver = await fixture.RegisterUserAsync();
        await sender.Client.PostAsJsonAsync($"/accounts/{sender.PrimaryAccount.AccountId}/topup", new { amount = 500m });
        await sender.Client.PostAsJsonAsync("/transfers/by-account-number", new
        {
            fromAccountId = sender.PrimaryAccount.AccountId,
            toAccountNumber = receiver.PrimaryAccount.AccountNumber,
            amount = 60m,
            saveBeneficiary = false,
        });

        var response = await sender.Client.GetAsync("/transactions/mine?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = (await response.Content.ReadFromJsonAsync<MyTransactionsResponse>(ApiIntegrationFixture.Json))!;
        history.Total.Should().BeGreaterThanOrEqualTo(2);
        history.Items.Should().Contain(t =>
            t.Type == "Transfer" && t.Amount == 60m &&
            t.DestinationAccountNumber == receiver.PrimaryAccount.AccountNumber);
    }

    [Fact]
    public async Task Transfers_Unauthenticated_Returns401()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/transfers/preview-by-number", new { accountNumber = "IR-9999999999" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

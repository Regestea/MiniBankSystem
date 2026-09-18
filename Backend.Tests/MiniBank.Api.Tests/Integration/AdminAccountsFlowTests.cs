using System.Net;
using System.Net.Http.Json;

namespace MiniBank.Api.Tests.Integration;

/// <summary>
/// End-to-end admin account lifecycle: approve / reject / freeze / unfreeze,
/// plus role enforcement (anonymous 401, non-admin 403).
/// </summary>
[Collection("api-integration")]
public sealed class AdminAccountsFlowTests(ApiIntegrationFixture fixture)
{
    [Fact]
    public async Task Approve_PendingAccount_ActivatesIt_AndTopupWorks()
    {
        var user = await fixture.RegisterUserAsync();
        var admin = await fixture.AdminSessionAsync();
        var opened = await user.Client.PostAsJsonAsync("/accounts", new { accountType = "Current" });
        var pending = (await opened.Content.ReadFromJsonAsync<AccountResponseDto>(ApiIntegrationFixture.Json))!;

        var approve = await admin.Client.PostAsync($"/admin/accounts/{pending.AccountId}/approve", null);

        approve.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = (await approve.Content.ReadFromJsonAsync<AccountStatusDto>(ApiIntegrationFixture.Json))!;
        approved.Status.Should().Be("Active");

        var topup = await user.Client.PostAsJsonAsync($"/accounts/{pending.AccountId}/topup", new { amount = 50m });
        topup.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reject_PendingAccount_ClosesIt()
    {
        var user = await fixture.RegisterUserAsync();
        var admin = await fixture.AdminSessionAsync();
        var opened = await user.Client.PostAsJsonAsync("/accounts", new { accountType = "Savings" });
        var pending = (await opened.Content.ReadFromJsonAsync<AccountResponseDto>(ApiIntegrationFixture.Json))!;

        var reject = await admin.Client.PostAsJsonAsync(
            $"/admin/accounts/{pending.AccountId}/reject", new { reason = "Incomplete documents" });

        reject.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Freeze_BlocksTopup_AndUnfreeze_RestoresIt()
    {
        var user = await fixture.RegisterUserAsync();
        var admin = await fixture.AdminSessionAsync();
        var accountId = user.PrimaryAccount.AccountId;

        var freeze = await admin.Client.PostAsync($"/admin/accounts/{accountId}/freeze", null);
        freeze.StatusCode.Should().Be(HttpStatusCode.OK);

        var blocked = await user.Client.PostAsJsonAsync($"/accounts/{accountId}/topup", new { amount = 10m });
        blocked.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var unfreeze = await admin.Client.PostAsync($"/admin/accounts/{accountId}/unfreeze", null);
        unfreeze.StatusCode.Should().Be(HttpStatusCode.OK);

        var topup = await user.Client.PostAsJsonAsync($"/accounts/{accountId}/topup", new { amount = 10m });
        topup.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Approve_AsNonAdmin_Returns403_AndAnonymous_Returns401()
    {
        var user = await fixture.RegisterUserAsync();
        var other = await fixture.RegisterUserAsync();
        var opened = await user.Client.PostAsJsonAsync("/accounts", new { accountType = "Current" });
        var pending = (await opened.Content.ReadFromJsonAsync<AccountResponseDto>(ApiIntegrationFixture.Json))!;

        var forbidden = await other.Client.PostAsync($"/admin/accounts/{pending.AccountId}/approve", null);
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var anonymous = fixture.CreateClient();
        var unauthorized = await anonymous.PostAsync($"/admin/accounts/{pending.AccountId}/approve", null);
        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

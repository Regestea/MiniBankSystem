using System.Net;
using System.Net.Http.Json;

namespace MiniBank.Api.Tests.Integration;

/// <summary>
/// End-to-end saved-beneficiaries (recipients) journeys.
/// </summary>
[Collection("api-integration")]
public sealed class BeneficiariesJourneyTests(ApiIntegrationFixture fixture)
{
    [Fact]
    public async Task Add_Returns201_AndAppearsInList()
    {
        var owner = await fixture.RegisterUserAsync();
        var other = await fixture.RegisterUserAsync(fullName: "Buddy Holder");

        var response = await owner.Client.PostAsJsonAsync("/beneficiaries", new
        {
            accountNumber = other.PrimaryAccount.AccountNumber,
            nickname = "Buddy",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await response.Content.ReadFromJsonAsync<BeneficiaryDto>(ApiIntegrationFixture.Json))!;
        created.AccountNumber.Should().Be(other.PrimaryAccount.AccountNumber);

        var list = await owner.Client.GetFromJsonAsync<List<BeneficiaryDto>>(
            "/beneficiaries", ApiIntegrationFixture.Json);
        list.Should().ContainSingle(b => b.BeneficiaryId == created.BeneficiaryId);
    }

    [Fact]
    public async Task Add_Duplicate_Returns409()
    {
        var owner = await fixture.RegisterUserAsync();
        var other = await fixture.RegisterUserAsync();
        var payload = new
        {
            accountNumber = other.PrimaryAccount.AccountNumber,
            nickname = (string?)null,
        };

        var first = await owner.Client.PostAsJsonAsync("/beneficiaries", payload);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await owner.Client.PostAsJsonAsync("/beneficiaries", payload);

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Add_UnknownAccount_Returns404()
    {
        var owner = await fixture.RegisterUserAsync();

        var response = await owner.Client.PostAsJsonAsync("/beneficiaries", new
        {
            accountNumber = "IR-9999999999",
            nickname = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Add_InvalidFormat_Returns400()
    {
        var owner = await fixture.RegisterUserAsync();

        var response = await owner.Client.PostAsJsonAsync("/beneficiaries", new
        {
            accountNumber = "bad-number",
            nickname = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_RemovesBeneficiary_AndSecondDelete_Returns404()
    {
        var owner = await fixture.RegisterUserAsync();
        var other = await fixture.RegisterUserAsync();
        var created = await owner.Client.PostAsJsonAsync("/beneficiaries", new
        {
            accountNumber = other.PrimaryAccount.AccountNumber,
            nickname = (string?)null,
        });
        var dto = (await created.Content.ReadFromJsonAsync<BeneficiaryDto>(ApiIntegrationFixture.Json))!;

        var delete = await owner.Client.DeleteAsync($"/beneficiaries/{dto.BeneficiaryId}");

        delete.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await owner.Client.GetFromJsonAsync<List<BeneficiaryDto>>(
            "/beneficiaries", ApiIntegrationFixture.Json);
        list.Should().BeEmpty();

        var again = await owner.Client.DeleteAsync($"/beneficiaries/{dto.BeneficiaryId}");
        again.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Beneficiaries_Unauthenticated_Returns401()
    {
        var client = fixture.CreateClient();

        (await client.GetAsync("/beneficiaries")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

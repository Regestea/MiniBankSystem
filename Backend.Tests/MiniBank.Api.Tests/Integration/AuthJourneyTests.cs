using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MiniBank.Api.Tests.Integration;

/// <summary>
/// End-to-end auth + profile journeys through the real stack
/// (Identity, Mediator, handlers, PostgreSQL).
/// </summary>
[Collection("api-integration")]
public sealed class AuthJourneyTests(ApiIntegrationFixture fixture)
{
    [Fact]
    public async Task Register_Returns201_WithCustomerAndLocation()
    {
        var email = $"{Guid.NewGuid():N}@test.local";
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/customers/register", new
        {
            email,
            password = "Test123!",
            fullName = "Sara Ahmadi",
            phoneNumber = "09123456789",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var body = (await response.Content.ReadFromJsonAsync<CustomerResponse>(ApiIntegrationFixture.Json))!;
        body.Email.Should().Be(email);
        body.FullName.Should().Be("Sara Ahmadi");
        body.Status.Should().Be("Verified");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var session = await fixture.RegisterUserAsync();
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/customers/register", new
        {
            email = session.Email,
            password = "Test123!",
            fullName = "Someone Else",
            phoneNumber = "09123456789",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/customers/register", new
        {
            email = $"{Guid.NewGuid():N}@test.local",
            password = "weak",
            fullName = "Weak Pass",
            phoneNumber = "09123456789",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_InvalidPhone_Returns400()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/customers/register", new
        {
            email = $"{Guid.NewGuid():N}@test.local",
            password = "Test123!",
            fullName = "Bad Phone",
            phoneNumber = "not-a-phone",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IdentityRegisterEndpoint_StaysDisabled()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/register", new
        {
            email = $"{Guid.NewGuid():N}@test.local",
            password = "Test123!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var session = await fixture.RegisterUserAsync();
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/login", new { email = session.Email, password = "Wrong123!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithValidToken_Returns200_AndNewTokenWorks()
    {
        var session = await fixture.RegisterUserAsync();
        var client = fixture.CreateClient();
        var tokens = await ApiIntegrationFixture.LoginAsync(client, session.Email, session.Password);

        var refresh = await client.PostAsJsonAsync("/refresh", new { refreshToken = tokens.RefreshToken });

        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var renewed = (await refresh.Content.ReadFromJsonAsync<LoginResponse>(ApiIntegrationFixture.Json))!;
        renewed.AccessToken.Should().NotBeNullOrEmpty().And.NotBe(tokens.AccessToken);

        var probe = fixture.CreateClient();
        probe.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", renewed.AccessToken);
        (await probe.GetAsync("/customers/profile")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProfile_Unauthenticated_Returns401()
    {
        var client = fixture.CreateClient();

        (await client.GetAsync("/customers/profile")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/customers/overview")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_Authenticated_ReturnsOwnProfile()
    {
        var session = await fixture.RegisterUserAsync(fullName: "Profile User");

        var response = await session.Client.GetAsync("/customers/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<CustomerResponse>(ApiIntegrationFixture.Json))!;
        body.Email.Should().Be(session.Email);
        body.FullName.Should().Be("Profile User");
    }

    [Fact]
    public async Task UpdateProfile_ChangesNameAndPhone()
    {
        var session = await fixture.RegisterUserAsync();

        var response = await session.Client.PutAsJsonAsync("/customers/profile", new
        {
            fullName = "Updated Name",
            phoneNumber = "09998887766",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<CustomerResponse>(ApiIntegrationFixture.Json))!;
        body.FullName.Should().Be("Updated Name");
        body.PhoneNumber.Should().Be("09998887766");

        var overview = await session.Client.GetFromJsonAsync<OverviewResponse>(
            "/customers/overview", ApiIntegrationFixture.Json);
        overview!.FullName.Should().Be("Updated Name");
    }
}

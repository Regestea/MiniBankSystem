using System.Net;
using System.Net.Http.Json;

namespace MiniBank.Api.Tests;

[Collection("Sequential")]
public class KycControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public KycControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMock();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Submit_Returns401_WhenNotAuthenticated()
    {
        var command = new { CustomerId = Guid.NewGuid(), DocumentId = Guid.NewGuid() };
        var response = await _client.PostAsJsonAsync("/kyc/submit", command);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStatus_Returns401_WhenNotAuthenticated()
    {
        var customerId = Guid.NewGuid();
        var response = await _client.GetAsync($"/kyc/{customerId}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

[Collection("Sequential")]
public class AdminKycControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AdminKycControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMock();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Review_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var request = new { Approve = true, Reason = (string?)null };
        var response = await _client.PostAsJsonAsync($"/admin/kyc/{id}/review", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

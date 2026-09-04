using System.Net;
using System.Net.Http.Json;

namespace MiniBank.Api.Tests;

[Collection("Sequential")]
public class RiskControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public RiskControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMock();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRisk_Returns401_WhenNotAuthenticated()
    {
        var customerId = Guid.NewGuid();
        var response = await _client.GetAsync($"/risk/{customerId}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

[Collection("Sequential")]
public class AdminRiskControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AdminRiskControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMock();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UpdateLevel_Returns401_WhenNotAuthenticated()
    {
        var customerId = Guid.NewGuid();
        var request = new { RiskLevel = "High" };
        var response = await _client.PostAsJsonAsync($"/admin/risk/{customerId}/level", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListHighRisk_Returns401_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/admin/risk/high-risk");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

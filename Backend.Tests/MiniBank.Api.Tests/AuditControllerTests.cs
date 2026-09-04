using System.Net;
using System.Net.Http.Json;

namespace MiniBank.Api.Tests;

[Collection("Sequential")]
public class AuditControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuditControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMock();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLogs_Returns401_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/admin/audit/logs");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

using System.Net;
using System.Net.Http.Json;

namespace MiniBank.Api.Tests;

[Collection("Sequential")]
public class DocumentsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public DocumentsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMock();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Upload_Returns401_WhenNotAuthenticated()
    {
        var command = new { FileName = "test.pdf", ContentType = "application/pdf", FileSize = 1024L, Type = "KYC" };
        var response = await _client.PostAsJsonAsync("/documents/upload", command);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDocument_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/documents/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListCustomerDocuments_Returns401_WhenNotAuthenticated()
    {
        var customerId = Guid.NewGuid();
        var response = await _client.GetAsync($"/customers/{customerId}/documents");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

[Collection("Sequential")]
public class AdminDocumentsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AdminDocumentsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMock();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task VerifyDocument_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var request = new { Approve = true, Reason = (string?)null };
        var response = await _client.PostAsJsonAsync($"/admin/documents/{id}/verify", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

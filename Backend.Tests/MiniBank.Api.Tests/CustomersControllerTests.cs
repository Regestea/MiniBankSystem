using System.Net;
using System.Net.Http.Json;
using MiniBank.Features.Customers;
using MiniBank.Features.Customers.GetCustomer;
using MiniBank.Features.Customers.GetCurrentCustomer;

namespace MiniBank.Api.Tests;

[Collection("Sequential")]
public class CustomersControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _unauthClient;
    private readonly HttpClient _authClient;
    private readonly TestWebApplicationFactory _factory;

    public CustomersControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMock();
        _unauthClient = factory.CreateClient();
        _authClient = factory.CreateAuthenticatedClient();
    }

    // --- 401 Unauthorized ---

    [Fact]
    public async Task GetCurrent_Returns401_WhenNotAuthenticated()
    {
        var response = await _unauthClient.GetAsync("/customers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var response = await _unauthClient.GetAsync($"/customers/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var request = new { FullName = "Updated Name", PhoneNumber = "+1234567890" };
        var response = await _unauthClient.PutAsJsonAsync($"/customers/{id}", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Happy path ---

    [Fact]
    public async Task GetCurrent_Returns200_WhenAuthenticated()
    {
        var userId = Guid.NewGuid();
        _factory.MockMediator.Send(Arg.Any<GetCurrentCustomerQuery>(), Arg.Any<CancellationToken>())
            .Returns(new CustomerDetailResponse(userId, "John Doe", "john@test.com", "+1234567890", "Verified", DateTimeOffset.UtcNow));

        var client = _factory.CreateAuthenticatedClient(userId);
        var response = await client.GetAsync("/customers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_Returns200_WhenValidId()
    {
        var customerId = Guid.NewGuid();
        _factory.MockMediator.Send(Arg.Any<GetCustomerQuery>(), Arg.Any<CancellationToken>())
            .Returns(new CustomerDetailResponse(customerId, "Jane Doe", "jane@test.com", "+0987654321", "Verified", DateTimeOffset.UtcNow));

        var response = await _authClient.GetAsync($"/customers/{customerId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<CustomerDetailResponse>();
        body.Should().NotBeNull();
        body!.CustomerId.Should().Be(customerId);
    }

    // --- 400 Bad Request ---

    [Fact]
    public async Task GetById_Returns404_WhenCustomerNotFound()
    {
        var customerId = Guid.NewGuid();
        _factory.MockMediator.Send(Arg.Any<GetCustomerQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CustomerDetailResponse>(
                new Domain.BuildingBlocks.Exceptions.NotFoundException("customer", customerId)));

        var response = await _authClient.GetAsync($"/customers/{customerId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

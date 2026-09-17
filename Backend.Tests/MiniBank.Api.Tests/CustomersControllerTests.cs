using System.Net;
using System.Net.Http.Json;
using MiniBank.Features.Customers;
using MiniBank.Features.Customers.GetCustomer;
using MiniBank.Features.Customers.GetCurrentCustomer;
using MiniBank.Features.Customers.UpdateCurrentCustomer;

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
    public async Task GetProfile_Returns401_WhenNotAuthenticated()
    {
        var response = await _unauthClient.GetAsync("/customers/profile");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_Returns401_WhenNotAuthenticated()
    {
        var request = new { FullName = "Updated Name", PhoneNumber = "09123456789" };
        var response = await _unauthClient.PutAsJsonAsync("/customers/profile", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Happy path ---

    [Fact]
    public async Task GetProfile_Returns200_WhenAuthenticated()
    {
        var userId = Guid.NewGuid();
        _factory.MockMediator.Send(Arg.Any<GetCurrentCustomerQuery>(), Arg.Any<CancellationToken>())
            .Returns(new CustomerDetailResponse(userId, "John Doe", "john@test.com", "09123456789", "Verified", DateTimeOffset.UtcNow));

        var client = _factory.CreateAuthenticatedClient(userId);
        var response = await client.GetAsync("/customers/profile");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateProfile_Returns200_WhenAuthenticated()
    {
        var userId = Guid.NewGuid();
        _factory.MockMediator.Send(Arg.Any<UpdateCurrentCustomerCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CustomerResponse(userId, "Updated Name", "john@test.com", "09123456789", "Verified", DateTimeOffset.UtcNow));

        var client = _factory.CreateAuthenticatedClient(userId);
        var request = new { FullName = "Updated Name", PhoneNumber = "09123456789" };
        var response = await client.PutAsJsonAsync("/customers/profile", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Identity comes from the token — the command must not carry any customer id.
        await _factory.MockMediator.Received(1).Send(
            Arg.Is<UpdateCurrentCustomerCommand>(c => c.FullName == "Updated Name" && c.PhoneNumber == "09123456789"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminGetCustomer_Returns200_WhenAdmin()
    {
        var customerId = Guid.NewGuid();
        _factory.MockMediator.Send(Arg.Any<GetCustomerQuery>(), Arg.Any<CancellationToken>())
            .Returns(new CustomerDetailResponse(customerId, "Jane Doe", "jane@test.com", "09123456789", "Verified", DateTimeOffset.UtcNow));

        var adminClient = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await adminClient.GetAsync($"/admin/customers/{customerId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<CustomerDetailResponse>();
        body.Should().NotBeNull();
        body!.CustomerId.Should().Be(customerId);
    }

    // --- 400 Bad Request ---

    [Fact]
    public async Task AdminGetCustomer_Returns404_WhenCustomerNotFound()
    {
        var customerId = Guid.NewGuid();
        _factory.MockMediator.Send(Arg.Any<GetCustomerQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CustomerDetailResponse>(
                new Domain.BuildingBlocks.Exceptions.NotFoundException("customer", customerId)));

        var adminClient = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await adminClient.GetAsync($"/admin/customers/{customerId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

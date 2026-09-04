using System.Net;
using System.Net.Http.Json;

namespace MiniBank.Api.Tests;

[Collection("Sequential")]
public class AdminControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _unauthClient;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _userClient;
    private readonly TestWebApplicationFactory _factory;

    public AdminControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMock();
        _unauthClient = factory.CreateClient();
        _adminClient = factory.CreateAuthenticatedClient(role: "Admin");
        _userClient = factory.CreateAuthenticatedClient(role: "User");
    }

    // --- 401 Unauthorized ---

    [Fact]
    public async Task ListCustomers_Returns401_WhenNotAuthenticated()
    {
        var response = await _unauthClient.GetAsync("/admin/customers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCustomer_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var response = await _unauthClient.GetAsync($"/admin/customers/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VerifyCustomer_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var response = await _unauthClient.PostAsJsonAsync($"/admin/customers/{id}/verify", (object?)null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BlockCustomer_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var response = await _unauthClient.PostAsJsonAsync($"/admin/customers/{id}/block", (object?)null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApproveAccount_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var response = await _unauthClient.PostAsJsonAsync($"/admin/accounts/{id}/approve", (object?)null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FreezeAccount_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var response = await _unauthClient.PostAsJsonAsync($"/admin/accounts/{id}/freeze", (object?)null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnfreezeAccount_Returns401_WhenNotAuthenticated()
    {
        var id = Guid.NewGuid();
        var response = await _unauthClient.PostAsJsonAsync($"/admin/accounts/{id}/unfreeze", (object?)null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BankReport_Returns401_WhenNotAuthenticated()
    {
        var response = await _unauthClient.GetAsync("/admin/reports/bank");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CustomerReport_Returns401_WhenNotAuthenticated()
    {
        var response = await _unauthClient.GetAsync("/admin/reports/customers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TransactionReport_Returns401_WhenNotAuthenticated()
    {
        var response = await _unauthClient.GetAsync("/admin/reports/transactions");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task KycReport_Returns401_WhenNotAuthenticated()
    {
        var response = await _unauthClient.GetAsync("/admin/reports/kyc");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- 403 Forbidden ---

    [Fact]
    public async Task ListCustomers_Returns403_WhenNotAdmin()
    {
        var response = await _userClient.GetAsync("/admin/customers");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VerifyCustomer_Returns403_WhenNotAdmin()
    {
        var id = Guid.NewGuid();
        var response = await _userClient.PostAsJsonAsync($"/admin/customers/{id}/verify", (object?)null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BlockCustomer_Returns403_WhenNotAdmin()
    {
        var id = Guid.NewGuid();
        var response = await _userClient.PostAsJsonAsync($"/admin/customers/{id}/block", (object?)null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ApproveAccount_Returns403_WhenNotAdmin()
    {
        var id = Guid.NewGuid();
        var response = await _userClient.PostAsJsonAsync($"/admin/accounts/{id}/approve", (object?)null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BankReport_Returns403_WhenNotAdmin()
    {
        var response = await _userClient.GetAsync("/admin/reports/bank");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Happy path (admin) ---

    [Fact]
    public async Task ListCustomers_Returns200_WhenAdmin()
    {
        _factory.MockMediator.Send(
                Arg.Any<Features.Customers.ListCustomers.ListCustomersQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(new Features.Customers.ListCustomers.CustomersPageResponse([], 1, 20, 0));

        var response = await _adminClient.GetAsync("/admin/customers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BankReport_Returns200_WhenAdmin()
    {
        _factory.MockMediator.Send(Arg.Any<Features.Reports.GetBankReport.GetBankReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Features.Reports.GetBankReport.BankReportResponse(0, 0, 0, 0m));

        var response = await _adminClient.GetAsync("/admin/reports/bank");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task KycReport_Returns200_WhenAdmin()
    {
        _factory.MockMediator.Send(Arg.Any<Features.Reports.GetKycReport.GetKycReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Features.Reports.GetKycReport.KycReportResponse(0, 0, 0, 0, 0, 0, 0, 0));

        var response = await _adminClient.GetAsync("/admin/reports/kyc");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

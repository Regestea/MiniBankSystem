using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiniBank.Infrastructure.Identity;
using MiniBank.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace MiniBank.Api.Tests.Integration;

/// <summary>
/// Real application host for integration tests: production Mediator, handlers,
/// validators and Identity — only the database points at a Testcontainer.
/// Unlike <see cref="TestWebApplicationFactory"/> (mocked IMediator + InMemory),
/// every request here exercises the full stack down to PostgreSQL (EF Core + Dapper).
/// </summary>
public sealed class IntegrationWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Real PostgreSQL (testcontainer) instead of the configured database.
            services.RemoveAll<DbContextOptions<MiniBankDbContext>>();
            services.RemoveAll<MiniBankDbContext>();
            services.AddDbContext<MiniBankDbContext>(options => options.UseNpgsql(connectionString));

            // NpgsqlConnectionFactory (Dapper query handlers) reads IConfiguration —
            // swap the singleton so Dapper hits the same testcontainer database.
            // Also seeds the admin credentials consumed by AdminSeeder.
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:minibankdb"] = connectionString,
                    ["Seed:Admin:Email"] = ApiIntegrationFixture.AdminEmail,
                    ["Seed:Admin:Password"] = ApiIntegrationFixture.AdminPassword,
                })
                .Build();
            services.RemoveAll<IConfiguration>();
            services.AddSingleton<IConfiguration>(config);
        });
    }
}

[CollectionDefinition("api-integration")]
public sealed class ApiIntegrationCollection : ICollectionFixture<ApiIntegrationFixture> { }

/// <summary>
/// One PostgreSQL container + one migrated database + seeded roles/admin shared by
/// all integration tests (xUnit runs one collection sequentially). Tests isolate by
/// registering unique users per test — all customer data is scoped per user.
/// </summary>
public sealed class ApiIntegrationFixture : IAsyncLifetime
{
    public const string AdminEmail = "admin@integration.test";
    public const string AdminPassword = "Admin123!";

    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6-alpine")
        .WithDatabase("minibank_api_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private IntegrationWebApplicationFactory _factory = null!;

    public HttpClient CreateClient() => _factory.CreateClient();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new IntegrationWebApplicationFactory(_postgres.GetConnectionString());

        // Force host creation, then migrate + seed roles/admin (Program only does this in Development).
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MiniBankDbContext>();
        await db.Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AdminSeeder>().SeedAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    /// <summary>Registers a unique user and returns an authenticated session (real login tokens).</summary>
    public async Task<UserSession> RegisterUserAsync(string fullName = "Test User", string password = "Test123!")
    {
        var email = $"{Guid.NewGuid():N}@test.local";
        var anonymous = CreateClient();

        var register = await anonymous.PostAsJsonAsync("/customers/register", new
        {
            email,
            password,
            fullName,
            phoneNumber = "09123456789",
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        var registered = (await register.Content.ReadFromJsonAsync<CustomerResponse>(Json))!;
        var client = CreateClient();
        await LoginAsync(client, email, password);
        var overview = await client.GetFromJsonAsync<OverviewResponse>("/customers/overview", Json);

        return new UserSession(email, password, registered.CustomerId, client, overview!);
    }

    /// <summary>Real Identity login — puts the bearer token on the client.</summary>
    public static async Task<LoginResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var login = await client.PostAsJsonAsync("/login", new { email, password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = (await login.Content.ReadFromJsonAsync<LoginResponse>(Json))!;
        tokens.AccessToken.Should().NotBeNullOrEmpty();
        tokens.RefreshToken.Should().NotBeNullOrEmpty();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return tokens;
    }

    public async Task<UserSession> AdminSessionAsync()
    {
        var client = CreateClient();
        await LoginAsync(client, AdminEmail, AdminPassword);
        return new UserSession(AdminEmail, AdminPassword, Guid.Empty, client, null);
    }
}

public sealed record UserSession(
    string Email,
    string Password,
    Guid CustomerId,
    HttpClient Client,
    OverviewResponse? Overview)
{
    public OverviewAccount PrimaryAccount => Overview!.Accounts.First(a => a.Status == "Active");
}

// ---- API DTOs (mirror the backend records; deserialized case-insensitively) ----

public sealed record LoginResponse(string TokenType, string AccessToken, int ExpiresIn, string RefreshToken);

public sealed record CustomerResponse(
    Guid CustomerId, string FullName, string Email, string PhoneNumber, string Status, DateTimeOffset CreatedAt);

public sealed record OverviewAccount(
    Guid AccountId, string AccountNumber, string AccountType, string Status, decimal Balance, DateTimeOffset CreatedAt);

public sealed record AccountResponseDto(
    Guid AccountId, string AccountNumber, string AccountType, string Status, DateTimeOffset CreatedAt);

public sealed record OverviewResponse(
    Guid CustomerId, string FullName, string Email, string PhoneNumber, string Status,
    DateTimeOffset CreatedAt, List<OverviewAccount> Accounts, decimal TotalBalance);

public sealed record TransactionResponseDto(
    Guid TransactionId, string Type, decimal Amount, string ReferenceId, DateTimeOffset OccurredOn);

public sealed record TransferResponseDto(
    Guid TransactionId, decimal Amount, string ReferenceId, Guid FromAccountId, Guid ToAccountId, DateTimeOffset OccurredOn);

public sealed record PreviewResponseDto(
    Guid ToAccountId, string AccountNumber, string HolderFullName, string MaskedHolderName);

public sealed record MyTransactionDto(
    Guid TransactionId, string Type, decimal Amount, string? SourceAccountNumber,
    string? DestinationAccountNumber, DateTimeOffset OccurredOn, string ReferenceId, string? Description);

public sealed record MyTransactionsResponse(int Page, int PageSize, int Total, List<MyTransactionDto> Items);

public sealed record BeneficiaryDto(
    Guid BeneficiaryId, string AccountNumber, string HolderName, DateTimeOffset CreatedAt);

public sealed record StatementEntryDto(
    Guid LedgerEntryId, string Type, decimal Amount, DateTimeOffset OccurredOn, string? ReferenceId, string? Description);

public sealed record StatementResponseDto(
    Guid AccountId, string AccountNumber, string Status, decimal Balance,
    int Page, int PageSize, int Total, List<StatementEntryDto> Entries);

public sealed record AccountStatusDto(Guid AccountId, string Status, int Version);

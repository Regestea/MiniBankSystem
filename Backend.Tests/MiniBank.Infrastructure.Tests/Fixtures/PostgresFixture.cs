using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniBank.Infrastructure.Persistence;
using Npgsql;
using System.Data;
using Testcontainers.PostgreSql;

namespace MiniBank.Infrastructure.Tests.Fixtures;

/// <summary>
/// Shared PostgreSQL Testcontainer fixture. One container per test collection – fast, isolated, production-equivalent.
/// Uses real PostgreSQL + EF Core migrations, not InMemory. Aligns with DDD Infrastructure tests requirement.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18.6-alpine")
        .WithDatabase("minibank_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public IConfiguration Configuration => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:minibankdb"] = ConnectionString })
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply EF Core migrations to match production schema (Identity + domain tables)
        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public MiniBankDbContext CreateContext()
        => new(new DbContextOptionsBuilder<MiniBankDbContext>()
            .UseNpgsql(ConnectionString, o => o.EnableRetryOnFailure())
            .Options);

    public IDbConnection CreateConnection()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    /// <summary>Helper to truncate domain tables for isolation between tests.</summary>
    public async Task ClearDomainTablesAsync()
    {
        await using var ctx = CreateContext();
        // Order matters due to FKs: ledger_entries (owned) -> accounts -> transactions -> customers -> audit_logs
        await ctx.Database.ExecuteSqlRawAsync(
            @"TRUNCATE TABLE ""ledger_entries"", ""transactions"", ""accounts"", ""customers"", ""audit_logs"" CASCADE;");
    }

    /// <summary>Clears outbox messages for test isolation.</summary>
    public async Task ClearOutboxAsync()
    {
        await using var ctx = CreateContext();
        await ctx.Database.ExecuteSqlRawAsync(@"TRUNCATE TABLE ""outbox_messages"" CASCADE;");
    }

    /// <summary>
    /// Creates the IdentityUser row required by fk_customers_aspnet_user before a Customer
    /// with the same Guid can be inserted (same-Guid design).
    /// </summary>
    public async Task SeedIdentityUserAsync(Guid userId, string email)
    {
        await using var ctx = CreateContext();
        await ctx.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""AspNetUsers"" (""Id"", ""UserName"", ""NormalizedUserName"", ""Email"", ""NormalizedEmail"", ""EmailConfirmed"", ""PasswordHash"", ""SecurityStamp"", ""ConcurrencyStamp"", ""PhoneNumberConfirmed"", ""TwoFactorEnabled"", ""LockoutEnabled"", ""AccessFailedCount"")
              VALUES ({0}, {1}, {2}, {1}, {2}, true, '', {3}, {3}, false, false, false, 0)",
            userId, email, email.ToUpperInvariant(), Guid.NewGuid().ToString());
    }

    /// <summary>Truncates both domain and Identity tables – use when testing Customer ↔ IdentityUser 1:1 Guid link.</summary>
    public async Task ClearAllTablesAsync()
    {
        await using var ctx = CreateContext();
        await ctx.Database.ExecuteSqlRawAsync(
            @"TRUNCATE TABLE ""ledger_entries"", ""transactions"", ""accounts"", ""customers"", ""AspNetUsers"", ""AspNetRoles"", ""AspNetUserRoles"", ""AspNetUserClaims"", ""AspNetUserLogins"", ""AspNetRoleClaims"", ""AspNetUserTokens"" CASCADE;");
    }
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture> { }

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
        // Order matters due to FKs: ledger_entries (owned) -> accounts -> transactions -> customers
        await ctx.Database.ExecuteSqlRawAsync(
            @"TRUNCATE TABLE ""ledger_entries"", ""transactions"", ""accounts"", ""customers"" CASCADE;");
    }
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture> { }

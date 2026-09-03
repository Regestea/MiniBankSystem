using FluentAssertions;
using MiniBank.Infrastructure.Persistence;
using MiniBank.Infrastructure.Tests.Fixtures;

namespace MiniBank.Infrastructure.Tests.Persistence;

[Collection("postgres")]
public sealed class NpgsqlConnectionFactoryTests
{
    private readonly PostgresFixture _fixture;
    public NpgsqlConnectionFactoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateOpenConnection_Returns_Open_NpgsqlConnection_To_Same_Database_As_EfCore()
    {
        var factory = new NpgsqlConnectionFactory(_fixture.Configuration);

        using var conn = factory.CreateOpenConnection();

        conn.State.Should().Be(System.Data.ConnectionState.Open);
        conn.ConnectionString.Should().Contain("minibank_tests");

        // Verify EF Core and Dapper see same data
        // Insert via EF Core, query via Dapper connection
        var customer = MiniBank.Domain.CustomerAggregate.Customer.Create("Factory User", $"factory_{Guid.NewGuid():N}@test.com", "09123456789");
        using (var ctx = _fixture.CreateContext())
        {
            await _fixture.SeedIdentityUserAsync(customer.Id.Value, customer.Email);
            ctx.Customers.Add(customer);
            ctx.SaveChanges();
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM customers WHERE customer_id = @id";
        var p = cmd.CreateParameter(); p.ParameterName = "id"; p.Value = customer.Id.Value; cmd.Parameters.Add(p);
        var count = (long)cmd.ExecuteScalar()!;
        count.Should().Be(1);
    }

    [Fact]
    public void CreateOpenConnection_Throws_When_ConnectionString_Missing()
    {
        var emptyConfig = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var factory = new NpgsqlConnectionFactory(emptyConfig);

        var act = () => factory.CreateOpenConnection();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*minibankdb*");
    }

    [Fact]
    public void CreateOpenConnection_Each_Call_Returns_New_Connection()
    {
        var factory = new NpgsqlConnectionFactory(_fixture.Configuration);
        using var conn1 = factory.CreateOpenConnection();
        using var conn2 = factory.CreateOpenConnection();

        conn1.Should().NotBeSameAs(conn2);
        conn1.State.Should().Be(System.Data.ConnectionState.Open);
        conn2.State.Should().Be(System.Data.ConnectionState.Open);
    }
}

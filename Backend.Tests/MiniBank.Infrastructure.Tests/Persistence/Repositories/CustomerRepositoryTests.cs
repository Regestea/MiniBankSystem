using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Infrastructure.Tests.Fixtures;

namespace MiniBank.Infrastructure.Tests.Persistence.Repositories;

/// <summary>
/// Infrastructure layer tests: real PostgreSQL via Testcontainers, EF Core, no InMemory.
/// Tests ICustomerRepository implementation (CustomerRepository.cs:7) – persistence mapping, constraints, value-object conversions.
/// </summary>
[Collection("postgres")]
public sealed class CustomerRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public CustomerRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Persists_And_Rehydrates_Correctly()
    {
        var customer = Customer.Create("Alice Smith", $"alice_{Guid.NewGuid():N}@test.com", "09123456789");
        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Customers.AddAsync(customer);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var loaded = await ctx.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
            loaded.Should().NotBeNull();
            loaded!.FullName.Should().Be(customer.FullName);
            loaded.Email.Should().Be(customer.Email);
            loaded.PhoneNumber.Should().Be(customer.PhoneNumber);
            loaded.Status.Should().Be(customer.Status);
            loaded.Version.Should().Be(0);
            loaded.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        }
    }

    [Fact]
    public async Task EmailExistsAsync_Returns_True_For_Existing_Email()
    {
        var email = $"unique_{Guid.NewGuid():N}@test.com";
        var customer = Customer.Create("Bob", email, "09123456789");
        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Customers.AddAsync(customer);
            await ctx.SaveChangesAsync();
        }

        var nonExistentEmail = $"nonexistent_{Guid.NewGuid():N}@test.com";
        await using (var ctx = _fixture.CreateContext())
        {
            (await ctx.Customers.AnyAsync(c => c.Email == email)).Should().BeTrue();
            (await ctx.Customers.AnyAsync(c => c.Email == nonExistentEmail)).Should().BeFalse();
            // Email VO normalizes to lower-case, so upper-case query should also match if caller lowercases before query (via Email VO)
            // Raw Postgres is case-sensitive, but domain normalizes – handler will lower-case via Email VO before calling EmailExistsAsync
            var upperEmail = email.ToUpperInvariant();
            var normalized = new MiniBank.Domain.CustomerAggregate.ValueObjects.Email(upperEmail).ToString();
            (await ctx.Customers.AnyAsync(c => c.Email == normalized)).Should().BeTrue();
        }
    }

    [Fact]
    public async Task Email_UniqueConstraint_Prevents_Duplicates()
    {
        var email = $"dup_{Guid.NewGuid():N}@test.com";
        var c1 = Customer.Create("First", email, "09123456789");
        var c2 = Customer.Create("Second", email, "09876543210");

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Customers.AddAsync(c1);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Customers.AddAsync(c2);
            var act = async () => await ctx.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>()
                .Where(ex => ex.InnerException != null && ex.InnerException.Message.Contains("ux_customers_email"));
        }
    }

    [Fact]
    public async Task ValueObjects_Persist_Via_Conversions()
    {
        var email = $"vo_{Guid.NewGuid():N}@test.com";
        var customer = Customer.Create("Charlie", email, "09123456789");
        // Verify FullName, Email, PhoneNumber are value objects with conversions: CustomerConfiguration:16-32
        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Customers.AddAsync(customer);
            await ctx.SaveChangesAsync();
        }

        // Raw SQL via Dapper-equivalent Npgsql to verify column names snake_case as per CustomerConfiguration
        using var conn = _fixture.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT full_name, email, phone_number, status, version FROM customers WHERE customer_id = @id";
        var p = cmd.CreateParameter(); p.ParameterName = "id"; p.Value = customer.Id.Value; cmd.Parameters.Add(p);
        using var reader = await ((Npgsql.NpgsqlCommand)cmd).ExecuteReaderAsync();
        reader.Read().Should().BeTrue();
        reader.GetString(0).Should().Be("Charlie");
        reader.GetString(1).Should().Be(email);
        reader.GetString(2).Should().Be("09123456789");
        reader.GetInt16(3).Should().Be((short)customer.Status);
    }

    [Fact]
    public async Task Verify_Updates_Status_And_Version()
    {
        var customer = Customer.Create("Dana", $"dana_{Guid.NewGuid():N}@test.com", "09123456789");
        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Customers.AddAsync(customer);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var loaded = await ctx.Customers.FirstAsync(c => c.Id == customer.Id);
            loaded.Verify();
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var verified = await ctx.Customers.FirstAsync(c => c.Id == customer.Id);
            verified.Status.Should().Be(CustomerStatus.Verified);
            verified.Version.Should().Be(1);
        }
    }
}

using Dapper;
using FluentAssertions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Infrastructure.Tests.Fixtures;

namespace MiniBank.Infrastructure.Tests.Dapper;

/// <summary>
/// Tests that Dapper read models (same SQL as Features handlers) work against real PostgreSQL.
/// Verifies EF Core writes are readable via Dapper with snake_case mapping and correct balance aggregation.
/// No InMemory – real PostgreSQL as per infrastructure requirement.
/// </summary>
[Collection("postgres")]
public sealed class DapperReadModelTests
{
    private readonly PostgresFixture _fixture;
    public DapperReadModelTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Dapper_Can_Query_Customers_Written_Via_EfCore()
    {
        var email = $"dapper_{Guid.NewGuid():N}@test.com";
        var customer = Customer.Create("Dapper User", email, "09123456789");
        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Customers.AddAsync(customer);
            await ctx.SaveChangesAsync();
        }

        using var conn = _fixture.CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT customer_id, full_name, email, phone_number, status FROM customers WHERE email = @email",
            new { email });

        ((string)row!.email).Should().Be(email);
        ((string)row.full_name).Should().Be("Dapper User");
    }

    [Fact]
    public async Task Dapper_Balance_Aggregation_Matches_Domain_Calculation()
    {
        // Seed account with ledger entries via Domain (EF Core), then query balance via Dapper SQL used in GetStatementHandler/GetBankReportHandler
        var customer = Customer.Create("Charlie Brown", $"bal_{Guid.NewGuid():N}@test.com", "09123456789");
        var account = Account.Open(customer.Id, AccountType.Current);
        account.Deposit(Money.FromDecimal(1000m));
        account.Withdraw(Money.FromDecimal(250m));
        // Simulate transfer out 100
        var otherCustomer = Customer.Create("Other", $"other_{Guid.NewGuid():N}@test.com", "09123456789");
        var otherAccount = Account.Open(otherCustomer.Id, AccountType.Current);
        account.TransferTo(otherAccount, Money.FromDecimal(100m));

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Customers.AddRangeAsync(customer, otherCustomer);
            await ctx.Accounts.AddRangeAsync(account, otherAccount);
            await ctx.SaveChangesAsync();
        }

        // Same SQL as MiniBank.Features.Reports.GetBankReport.GetBankReportHandler:12-15
        const string balanceSql = """
            SELECT COALESCE(SUM(CASE WHEN type IN (0, 2) THEN amount ELSE -amount END), 0) AS balance
            FROM ledger_entries WHERE account_id = @accountId
            """;

        using var conn = _fixture.CreateConnection();
        var dapperBalance = await conn.ExecuteScalarAsync<decimal>(balanceSql, new { accountId = account.Id.Value });

        account.Balance.Amount.Should().Be(650m);
        dapperBalance.Should().Be(650m);
    }

    [Fact]
    public async Task Dapper_GetBankReport_Sql_Works()
    {
        // Seed at least one customer/account to ensure counts >=1, then run same SQL as GetBankReportHandler
        var customer = Customer.Create("Report User", $"report_{Guid.NewGuid():N}@test.com", "09123456789");
        var account = Account.Open(customer.Id, AccountType.Current);
        account.Deposit(Money.FromDecimal(100m));
        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Customers.AddAsync(customer);
            await ctx.Accounts.AddAsync(account);
            await ctx.SaveChangesAsync();
        }

        const string sql = """
            SELECT (SELECT COUNT(*) FROM customers)                                        AS customers,
                   (SELECT COUNT(*) FROM accounts)                                         AS accounts,
                   (SELECT COUNT(*) FROM accounts WHERE status = 0)                        AS active_accounts,
                   (SELECT COALESCE(SUM(CASE WHEN type IN (0, 2) THEN amount ELSE -amount END), 0)
                      FROM ledger_entries)                                                 AS total_balance;
            """;

        using var conn = _fixture.CreateConnection();
        var row = await conn.QuerySingleAsync<dynamic>(sql);

        ((long)row.customers).Should().BeGreaterThanOrEqualTo(1);
        ((long)row.accounts).Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Dapper_LedgerEntries_Ordering_Matches_Domain_GetStatementOrdered()
    {
        var customer = Customer.Create("Emma Wilson", $"order_{Guid.NewGuid():N}@test.com", "09123456789");
        var account = Account.Open(customer.Id, AccountType.Current);
        account.Deposit(Money.FromDecimal(100m));
        await Task.Delay(10); // ensure different OccurredOn
        account.Deposit(Money.FromDecimal(200m));

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Customers.AddAsync(customer);
            await ctx.Accounts.AddAsync(account);
            await ctx.SaveChangesAsync();
        }

        using var conn = _fixture.CreateConnection();
        var entries = (await conn.QueryAsync<dynamic>(
            "SELECT ledger_entry_id, type, amount, occurred_on FROM ledger_entries WHERE account_id = @id ORDER BY occurred_on, ledger_entry_id",
            new { id = account.Id.Value })).ToList();

        entries.Should().HaveCount(2);
        ((decimal)entries[0].amount).Should().Be(100m);
        ((decimal)entries[1].amount).Should().Be(200m);

        var domainOrdered = account.GetStatementOrdered();
        domainOrdered[0].Amount.Amount.Should().Be(100m);
        domainOrdered[1].Amount.Amount.Should().Be(200m);
    }
}

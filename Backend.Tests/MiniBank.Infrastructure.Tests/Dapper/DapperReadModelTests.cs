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

    [Fact]
    public async Task Dapper_GetAccounts_Ownership_FiltersCorrectly_SameGuidDesign()
    {
        // Validates the fixed GetAccountsHandler SQL: WHERE a.customer_id = @UserId (no AspNetUsers join)
        var customerA = Customer.Create("Alice Owner", $"ownA_{Guid.NewGuid():N}@test.com", "09123456789");
        var customerB = Customer.Create("Bob Stranger", $"ownB_{Guid.NewGuid():N}@test.com", "09123456789");
        var accountA1 = Account.Open(customerA.Id, AccountType.Current);
        var accountA2 = Account.Open(customerA.Id, AccountType.Savings);
        var accountB = Account.Open(customerB.Id, AccountType.Current);
        accountA1.Deposit(Money.FromDecimal(100m));
        accountB.Deposit(Money.FromDecimal(999m));

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Customers.AddRangeAsync(customerA, customerB);
            await ctx.Accounts.AddRangeAsync(accountA1, accountA2, accountB);
            await ctx.SaveChangesAsync();
        }

        const string getAccountsSql = """
            SELECT a.account_id,
                   a.account_number,
                   a.account_type,
                   a.status,
                   COALESCE(SUM(CASE WHEN e.type IN (0, 2) THEN e.amount ELSE -e.amount END), 0) AS balance,
                   a.created_at
            FROM   accounts a
            LEFT   JOIN ledger_entries e ON e.account_id = a.account_id
            WHERE  a.customer_id = @UserId
            GROUP  BY a.account_id, a.account_number, a.account_type, a.status, a.created_at
            ORDER  BY a.created_at;
            """;

        using var conn = _fixture.CreateConnection();
        var accountsForA = (await conn.QueryAsync<dynamic>(getAccountsSql, new { UserId = customerA.Id.Value })).ToList();
        var accountsForB = (await conn.QueryAsync<dynamic>(getAccountsSql, new { UserId = customerB.Id.Value })).ToList();
        var accountsForUnknown = (await conn.QueryAsync<dynamic>(getAccountsSql, new { UserId = Guid.NewGuid() })).ToList();

        accountsForA.Should().HaveCount(2);
        var idsA = accountsForA.Select(a => (Guid)a.account_id).ToList();
        idsA.Should().Contain(accountA1.Id.Value);
        idsA.Should().Contain(accountA2.Id.Value);
        accountsForB.Should().HaveCount(1);
        ((Guid)accountsForB[0].account_id).Should().Be(accountB.Id.Value);
        accountsForUnknown.Should().BeEmpty();

        // Ensure balances are correctly aggregated
        var balanceA1 = (decimal)accountsForA.First(a => (Guid)a.account_id == accountA1.Id.Value).balance;
        balanceA1.Should().Be(100m);
    }

    [Fact]
    public async Task Dapper_GetStatement_Ownership_FiltersCorrectly()
    {
        // Validates the fixed GetStatementHandler AccountSql: WHERE a.account_id=@AccountId AND a.customer_id=@RequesterUserId
        var owner = Customer.Create("Owner", $"stmtOwn_{Guid.NewGuid():N}@test.com", "09123456789");
        var stranger = Customer.Create("Stranger", $"stmtStr_{Guid.NewGuid():N}@test.com", "09123456789");
        var account = Account.Open(owner.Id, AccountType.Current);
        account.Deposit(Money.FromDecimal(500m));

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Customers.AddRangeAsync(owner, stranger);
            await ctx.Accounts.AddAsync(account);
            await ctx.SaveChangesAsync();
        }

        const string accountSql = """
            SELECT a.account_id, a.account_number, a.status,
                   COALESCE(SUM(CASE WHEN e.type IN (0, 2) THEN e.amount ELSE -e.amount END), 0) AS balance
            FROM   accounts a
            LEFT   JOIN ledger_entries e ON e.account_id = a.account_id
            WHERE  a.account_id = @AccountId AND a.customer_id = @RequesterUserId
            GROUP  BY a.account_id, a.account_number, a.status
            """;

        using var conn = _fixture.CreateConnection();
        dynamic? owned = await conn.QuerySingleOrDefaultAsync<dynamic>(accountSql, new { AccountId = account.Id.Value, RequesterUserId = owner.Id.Value });
        dynamic? notOwned = await conn.QuerySingleOrDefaultAsync<dynamic>(accountSql, new { AccountId = account.Id.Value, RequesterUserId = stranger.Id.Value });
        dynamic? notFound = await conn.QuerySingleOrDefaultAsync<dynamic>(accountSql, new { AccountId = Guid.NewGuid(), RequesterUserId = owner.Id.Value });

        ((object?)owned).Should().NotBeNull();
        ((decimal)owned!.balance).Should().Be(500m);
        ((object?)notOwned).Should().BeNull(); // stranger cannot see owner's statement
        ((object?)notFound).Should().BeNull();
    }

    [Fact]
    public async Task Dapper_CustomerId_Equals_IdentityUserId_GuidDesign()
    {
        // 1:1 same-Guid design: Customer.Id.Value must be usable as IdentityUser Id
        // Verify AspNetUsers.Id is uuid and can store same Guid as customers.customer_id
        var sharedId = Guid.NewGuid();
        var customer = Customer.Create("Guid User", $"guid_{sharedId:N}@test.com", "09123456789", new MiniBank.Domain.CustomerAggregate.ValueObjects.CustomerId(sharedId));

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Customers.AddAsync(customer);
            await ctx.SaveChangesAsync();

            // Insert IdentityUser with same Guid via raw context (simulates IdentityUser<Guid>)
            var user = new Microsoft.AspNetCore.Identity.IdentityUser<Guid>
            {
                Id = sharedId,
                UserName = $"guid_{sharedId:N}@test.com",
                NormalizedUserName = $"GUID_{sharedId:N}@TEST.COM",
                Email = $"guid_{sharedId:N}@test.com",
                NormalizedEmail = $"GUID_{sharedId:N}@TEST.COM",
                EmailConfirmed = true
            };
            ctx.Set<Microsoft.AspNetCore.Identity.IdentityUser<Guid>>().Add(user);
            await ctx.SaveChangesAsync();
        }

        using var conn = _fixture.CreateConnection();
        var customerId = await conn.ExecuteScalarAsync<Guid>("SELECT customer_id FROM customers WHERE customer_id=@id", new { id = sharedId });
        var identityId = await conn.ExecuteScalarAsync<Guid>(@"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Id""=@id", new { id = sharedId });

        customerId.Should().Be(sharedId);
        identityId.Should().Be(sharedId);
        customerId.Should().Be(identityId);
    }
}

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Infrastructure.Tests.Fixtures;

namespace MiniBank.Infrastructure.Tests.Persistence.Repositories;

[Collection("postgres")]
public sealed class AccountRepositoryTests
{
    private readonly PostgresFixture _fixture;
    public AccountRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    private async Task<Customer> SeedCustomerAsync()
    {
        var customer = Customer.Create("Alice Smith", $"cust_{Guid.NewGuid():N}@test.com", "09123456789");
        await using var ctx = _fixture.CreateContext();
        await _fixture.SeedIdentityUserAsync(customer.Id.Value, customer.Email);
        await ctx.Customers.AddAsync(customer);
        await ctx.SaveChangesAsync();
        return customer;
    }

    [Fact]
    public async Task AddAsync_And_LoadAsync_With_Ledger_Persists_Owned_Entries()
    {
        var customer = await SeedCustomerAsync();
        var account = Account.Open(customer.Id, AccountType.Current);
        var (tx, _) = account.Deposit(Money.FromDecimal(500m));

        await using (var ctx = _fixture.CreateContext())
        {
            // Need to attach customer? Account has FK to customer, but customer already persisted
            await ctx.Accounts.AddAsync(account);
            // Also persist transaction? In real flow, transaction is saved via ITransactionRepository,
            // but ledger entries are owned by account and saved via Account. Here we just test account.
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var loaded = await ctx.Accounts.Include(a => a.Ledger).FirstOrDefaultAsync(a => a.Id == account.Id);
            loaded.Should().NotBeNull();
            loaded!.AccountNumber.Should().Be(account.AccountNumber);
            loaded.CustomerId.Should().Be(customer.Id);
            loaded.Status.Should().Be(AccountStatus.Active);
            loaded.Ledger.Should().HaveCount(1);
            loaded.Balance.Amount.Should().Be(500m);
            loaded.Ledger.First().Amount.Amount.Should().Be(500m);
            loaded.Version.Should().Be(1); // Deposit increments version: Account.cs:58,142
        }
    }

    [Fact]
    public async Task Deposit_And_Withdraw_Persist_Balance_Calculation()
    {
        var customer = await SeedCustomerAsync();
        var account = Account.Open(customer.Id, AccountType.Savings);
        account.Deposit(Money.FromDecimal(1000m));
        account.Withdraw(Money.FromDecimal(300m));

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Accounts.AddAsync(account);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var loaded = await ctx.Accounts.Include(a => a.Ledger).FirstAsync(a => a.Id == account.Id);
            loaded.Balance.Amount.Should().Be(700m);
            loaded.Ledger.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task Transfer_Persists_DoubleEntry_Across_Accounts()
    {
        var customer1 = await SeedCustomerAsync();
        var customer2 = await SeedCustomerAsync();
        var from = Account.Open(customer1.Id, AccountType.Current);
        var to = Account.Open(customer2.Id, AccountType.Current);
        from.Deposit(Money.FromDecimal(1000m));
        from.TransferTo(to, Money.FromDecimal(400m));

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Accounts.AddRangeAsync(from, to);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var loadedFrom = await ctx.Accounts.Include(a => a.Ledger).FirstAsync(a => a.Id == from.Id);
            var loadedTo = await ctx.Accounts.Include(a => a.Ledger).FirstAsync(a => a.Id == to.Id);
            loadedFrom.Balance.Amount.Should().Be(600m);
            loadedTo.Balance.Amount.Should().Be(400m);
            loadedFrom.Ledger.Should().HaveCount(2); // Deposit + TransferOut
            loadedTo.Ledger.Should().HaveCount(1); // TransferIn
        }
    }

    [Fact]
    public async Task AccountNumber_UniqueConstraint_Enforced()
    {
        var customer = await SeedCustomerAsync();
        var accountNumber = AccountNumber.Generate();
        var a1 = Account.Open(customer.Id, AccountType.Current, accountNumber);
        var a2 = Account.Open(customer.Id, AccountType.Savings, accountNumber); // same number

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Accounts.AddAsync(a1);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Accounts.AddAsync(a2);
            var act = async () => await ctx.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>()
                .Where(ex => ex.InnerException!.Message.Contains("ux_accounts_number"));
        }
    }

    [Fact]
    public async Task Freeze_And_Unfreeze_Persist_Status()
    {
        var customer = await SeedCustomerAsync();
        var account = Account.Open(customer.Id, AccountType.Current);

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Accounts.AddAsync(account);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var loaded = await ctx.Accounts.FirstAsync(a => a.Id == account.Id);
            loaded.Freeze();
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var frozen = await ctx.Accounts.FirstAsync(a => a.Id == account.Id);
            frozen.Status.Should().Be(AccountStatus.Frozen);
            frozen.Unfreeze();
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var active = await ctx.Accounts.FirstAsync(a => a.Id == account.Id);
            active.Status.Should().Be(AccountStatus.Active);
        }
    }

    [Fact]
    public async Task ConcurrencyToken_Version_Detects_Conflict()
    {
        var customer = await SeedCustomerAsync();
        var account = Account.Open(customer.Id, AccountType.Current);
        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Accounts.AddAsync(account);
            await ctx.SaveChangesAsync();
        }

        // Simulate concurrent edits
        await using var ctx1 = _fixture.CreateContext();
        await using var ctx2 = _fixture.CreateContext();
        var acc1 = await ctx1.Accounts.FirstAsync(a => a.Id == account.Id);
        var acc2 = await ctx2.Accounts.FirstAsync(a => a.Id == account.Id);

        acc1.Freeze();
        await ctx1.SaveChangesAsync(); // version 1

        acc2.Freeze(); // also tries to freeze from version 0
        var act = async () => await ctx2.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task Ledger_Owned_Mapping_Uses_SnakeCase_ColumnNames()
    {
        var customer = await SeedCustomerAsync();
        var account = Account.Open(customer.Id, AccountType.Current);
        account.Deposit(Money.FromDecimal(123.45m));

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Accounts.AddAsync(account);
            await ctx.SaveChangesAsync();
        }

        // Verify via raw SQL that Dapper will see same columns: AccountConfiguration:55-85
        using var conn = _fixture.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT amount, type, reference_id FROM ledger_entries WHERE account_id = @id";
        var p = cmd.CreateParameter(); p.ParameterName = "id"; p.Value = account.Id.Value; cmd.Parameters.Add(p);
        using var reader = await ((Npgsql.NpgsqlCommand)cmd).ExecuteReaderAsync();
        reader.Read().Should().BeTrue();
        reader.GetDecimal(0).Should().Be(123.45m);
        reader.GetInt16(1).Should().Be((short)MiniBank.Domain.Ledger.LedgerEntryType.Deposit);
    }
}

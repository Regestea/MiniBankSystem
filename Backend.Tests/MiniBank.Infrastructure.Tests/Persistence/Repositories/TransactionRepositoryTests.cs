using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Infrastructure.Tests.Fixtures;

namespace MiniBank.Infrastructure.Tests.Persistence.Repositories;

[Collection("postgres")]
public sealed class TransactionRepositoryTests
{
    private readonly PostgresFixture _fixture;
    public TransactionRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    private async Task<Account> SeedAccountWithBalanceAsync(decimal balance = 1000m)
    {
        var customer = Customer.Create("Bob Jones", $"c_{Guid.NewGuid():N}@test.com", "09123456789");
        var account = Account.Open(customer.Id, AccountType.Current);
        account.Approve();
        if (balance > 0) account.Deposit(Money.FromDecimal(balance));

        await using var ctx = _fixture.CreateContext();
        await _fixture.SeedIdentityUserAsync(customer.Id.Value, customer.Email);
        await ctx.Customers.AddAsync(customer);
        await ctx.Accounts.AddAsync(account);
        await ctx.SaveChangesAsync();
        return account;
    }

    [Fact]
    public async Task AddAsync_DepositTransaction_Persists()
    {
        var account = await SeedAccountWithBalanceAsync(500m);
        var tx = Transaction.CreateDeposit(account.Id, Money.FromDecimal(250m));

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Transactions.AddAsync(tx);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var loaded = await ctx.Transactions.FirstOrDefaultAsync(t => t.Id == tx.Id);
            loaded.Should().NotBeNull();
            loaded!.Amount.Amount.Should().Be(250m);
            loaded.Type.Should().Be(TransactionType.Deposit);
            loaded.DestinationAccountId.Should().Be(account.Id);
            loaded.SourceAccountId.Should().BeNull();
        }
    }

    [Fact]
    public async Task AddAsync_TransferTransaction_Persists_Both_Sides()
    {
        var from = await SeedAccountWithBalanceAsync(1000m);
        var toCustomer = Customer.Create("David Wilson", $"to_{Guid.NewGuid():N}@test.com", "09123456789");
        var to = Account.Open(toCustomer.Id, AccountType.Current);
        to.Approve();
        await using (var ctx = _fixture.CreateContext())
        {
            await _fixture.SeedIdentityUserAsync(toCustomer.Id.Value, toCustomer.Email);
            await ctx.Customers.AddAsync(toCustomer);
            await ctx.Accounts.AddAsync(to);
            await ctx.SaveChangesAsync();
        }

        var (tx, _, _) = from.TransferTo(to, Money.FromDecimal(300m));

        await using (var ctx = _fixture.CreateContext())
        {
            // Accounts already tracked? Need to attach or re-load – but transaction is independent
            await ctx.Transactions.AddAsync(tx);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            var loaded = await ctx.Transactions.FirstAsync(t => t.Id == tx.Id);
            loaded.Type.Should().Be(TransactionType.Transfer);
            loaded.SourceAccountId.Should().Be(from.Id);
            loaded.DestinationAccountId.Should().Be(to.Id);
            loaded.ReferenceId.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task ReferenceId_UniqueConstraint_Enforced()
    {
        var account = await SeedAccountWithBalanceAsync(1000m);
        var tx1 = Transaction.CreateDeposit(account.Id, Money.FromDecimal(10m), referenceId: $"REF-{Guid.NewGuid():N}");
        var sameRef = tx1.ReferenceId;
        var tx2 = Transaction.CreateDeposit(account.Id, Money.FromDecimal(20m), referenceId: sameRef);

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Transactions.AddAsync(tx1);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Transactions.AddAsync(tx2);
            var act = async () => await ctx.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>()
                .Where(ex => ex.InnerException!.Message.Contains("ux_transactions_reference"));
        }
    }

    [Fact]
    public async Task CheckConstraints_Enforce_Valid_Sides_And_Amount()
    {
        // Attempt to insert invalid transaction via raw SQL to verify constraints: TransactionConfiguration:16-22
        using var conn = _fixture.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO transactions (transaction_id, type, amount, source_account_id, destination_account_id, occurred_on, reference_id, version, created_at, updated_at)
            VALUES (@id, 0, -10, NULL, @dest, NOW(), @ref, 0, NOW(), NOW())
            """;
        var p1 = cmd.CreateParameter(); p1.ParameterName = "id"; p1.Value = Guid.NewGuid(); cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter(); p2.ParameterName = "dest"; p2.Value = Guid.NewGuid(); cmd.Parameters.Add(p2);
        var p3 = cmd.CreateParameter(); p3.ParameterName = "ref"; p3.Value = Guid.NewGuid().ToString("N"); cmd.Parameters.Add(p3);

        var act = async () => await ((Npgsql.NpgsqlCommand)cmd).ExecuteNonQueryAsync();
        await act.Should().ThrowAsync<Npgsql.PostgresException>()
            .Where(ex => ex.ConstraintName == "ck_transactions_amount_positive");
    }
}

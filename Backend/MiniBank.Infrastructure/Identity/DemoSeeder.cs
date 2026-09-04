using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.RiskAggregate;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Infrastructure.Persistence;

namespace MiniBank.Infrastructure.Identity;

/// <summary>
/// Development-only demo data: two verified customers with funded accounts and one
/// transfer, so reviewers have something to click in Scalar without manual setup.
/// Idempotent: skips anything that already exists (stable emails + reference ids).
/// Never run in Production.
/// </summary>
public sealed class DemoSeeder(
    UserManager<IdentityUser<Guid>> userManager,
    MiniBankDbContext db,
    MiniBank.Domain.BuildingBlocks.IUnitOfWork unitOfWork,
    ILogger<DemoSeeder> logger)
{
    private const string DemoPassword = "Demo123!";
    private static readonly Guid DemoUser1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DemoUser2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var user1 = await EnsureUserAsync(DemoUser1Id, "demo@minibank.local", cancellationToken);
        var user2 = await EnsureUserAsync(DemoUser2Id, "sara@minibank.local", cancellationToken);
        if (user1 is null || user2 is null)
            return;

        var customer1 = await EnsureCustomerAsync(DemoUser1Id, "Demo User", "demo@minibank.local", "09123456789", cancellationToken);
        var customer2 = await EnsureCustomerAsync(DemoUser2Id, "Sara Demo", "sara@minibank.local", "09987654321", cancellationToken);
        if (customer1 is null || customer2 is null)
            return;

        // Use UnitOfWork so demo writes also produce audit logs + dispatch domain events.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Accounts + money (only when nothing exists yet for these customers).
        var cust1Id = new MiniBank.Domain.CustomerAggregate.ValueObjects.CustomerId(DemoUser1Id);
        var cust2Id = new MiniBank.Domain.CustomerAggregate.ValueObjects.CustomerId(DemoUser2Id);
        var hasAccounts = await db.Accounts.AnyAsync(
            a => a.CustomerId == cust1Id || a.CustomerId == cust2Id, cancellationToken);
        if (hasAccounts)
            return;

        var acc1 = Account.Open(new(DemoUser1Id), AccountType.Current);
        acc1.Approve();
        var acc2 = Account.Open(new(DemoUser2Id), AccountType.Current);
        acc2.Approve();

        var (depositTx1, _) = acc1.Deposit(Money.FromDecimal(5000m), "Demo seed deposit", "demo-deposit-1");
        var (depositTx2, _) = acc2.Deposit(Money.FromDecimal(2500m), "Demo seed deposit", "demo-deposit-2");

        await db.Accounts.AddAsync(acc1, cancellationToken);
        await db.Accounts.AddAsync(acc2, cancellationToken);
        await db.Transactions.AddAsync(depositTx1, cancellationToken);
        await db.Transactions.AddAsync(depositTx2, cancellationToken);

        // One demo transfer so the statement/report endpoints are non-empty.
        var (transferTx, _, toEntry) = acc1.TransferTo(acc2, Money.FromDecimal(250m), "Demo seed transfer", "demo-transfer-1");
        acc2.ApplyInboundEntry(toEntry);
        await db.Transactions.AddAsync(transferTx, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // NOTE: password is documented in README/Backend.http for local demo login;
        // never log secrets — log accounts and balances only.
        logger.LogInformation(
            "Seeded demo data: demo@minibank.local / sara@minibank.local. Balances: acc1={Bal1}, acc2={Bal2}.",
            acc1.Balance.Amount, acc2.Balance.Amount);
    }

    private async Task<IdentityUser<Guid>?> EnsureUserAsync(Guid userId, string email, CancellationToken ct)
    {
        var existing = await userManager.FindByIdAsync(userId.ToString());
        if (existing is not null)
            return existing;
        if (await userManager.FindByEmailAsync(email) is not null)
            return await userManager.FindByEmailAsync(email);

        var user = new IdentityUser<Guid>
        {
            Id = userId,
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
        {
            logger.LogWarning("DemoSeeder: could not create {Email}: {Errors}",
                email, string.Join("; ", result.Errors.Select(e => e.Description)));
            return null;
        }

        await userManager.AddToRoleAsync(user, "User");
        return user;
    }

    private async Task<Customer?> EnsureCustomerAsync(Guid customerId, string fullName, string email, string phone, CancellationToken ct)
    {
        var id = new MiniBank.Domain.CustomerAggregate.ValueObjects.CustomerId(customerId);
        var existing = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (existing is not null)
            return existing;

        var customer = Customer.Create(fullName, email, phone, new(customerId));
        customer.Verify();
        await db.Customers.AddAsync(customer, ct);

        if (!await db.CustomerRisks.AnyAsync(r => r.CustomerId == customerId, ct))
            await db.CustomerRisks.AddAsync(CustomerRisk.Create(customerId), ct);

        return customer;
    }
}

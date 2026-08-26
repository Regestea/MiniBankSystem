using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Infrastructure.Identity;

namespace MiniBank.Infrastructure.Persistence;

/// <summary>
/// Single write-side context: domain aggregates + ASP.NET Core Identity tables.
/// </summary>
public sealed class MiniBankDbContext(DbContextOptions<MiniBankDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Identity mappings

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MiniBankDbContext).Assembly);
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.TransactionAggregate;

namespace MiniBank.Infrastructure.Persistence;

/// <summary>Write-side context: domain + Identity tables using Guid keys.</summary>
public sealed class MiniBankDbContext(DbContextOptions<MiniBankDbContext> options)
    : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Identity mappings with Guid keys

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MiniBankDbContext).Assembly);
    }
}

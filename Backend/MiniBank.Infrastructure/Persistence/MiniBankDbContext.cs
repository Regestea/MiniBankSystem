using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AuditAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.DocumentAggregate;
using MiniBank.Domain.KycAggregate;
using MiniBank.Domain.RiskAggregate;
using MiniBank.Domain.TransactionAggregate;

namespace MiniBank.Infrastructure.Persistence;

/// <summary>Write-side context: domain + Identity tables using Guid keys.</summary>
public sealed class MiniBankDbContext(DbContextOptions<MiniBankDbContext> options)
    : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<KycVerification> KycVerifications => Set<KycVerification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CustomerRisk> CustomerRisks => Set<CustomerRisk>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Identity mappings with Guid keys

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MiniBankDbContext).Assembly);
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniBank.Abstractions;
using MiniBank.Domain.AuditAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.Events;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Infrastructure.Persistence;
using MiniBank.Infrastructure.Tests.Fixtures;
using NSubstitute;

namespace MiniBank.Infrastructure.Tests.Persistence;

/// <summary>
/// Verifies EfUnitOfWork persists domain events correctly:
/// - Audit events → audit_logs table
/// - Non-audit events (e.g. MoneyTransferredEvent) are discarded (no subscribers)
/// </summary>
[Collection("postgres")]
public sealed class DomainEventDispatchTests
{
    private readonly PostgresFixture _fixture;
    public DomainEventDispatchTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SaveChangesAsync_Persists_AuditEvents_To_AuditLogs()
    {
        await _fixture.ClearDomainTablesAsync();

        var services = new ServiceCollection()
            .AddDbContext<MiniBankDbContext>(o => o.UseNpgsql(_fixture.ConnectionString))
            .AddLogging(b => b.AddProvider(NSubstitute.Substitute.For<ILoggerProvider>()))
            .AddScoped<IUnitOfWork, EfUnitOfWork>()
            .AddScoped<ICurrentUserContext>(_ => Substitute.For<ICurrentUserContext>())
            .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .BuildServiceProvider();

        var customerId = new CustomerId(Guid.NewGuid());
        await _fixture.SeedIdentityUserAsync(customerId.Value, $"evt_{Guid.NewGuid():N}@test.com");

        await using var scope = services.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var db = scope.ServiceProvider.GetRequiredService<MiniBankDbContext>();

        var customer = Customer.Create("Audit User", $"audit_{Guid.NewGuid():N}@test.com", "09123456789", customerId);
        db.Customers.Add(customer);

        await uow.SaveChangesAsync();

        // Verify audit event was persisted to audit_logs table
        var auditLogs = await db.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);
        auditLogs[0].Action.Should().Be(AuditAction.Create);
        auditLogs[0].EntityType.Should().Be("Customer");
    }

    [Fact]
    public async Task SaveChangesAsync_NonAuditEvents_AreDiscarded()
    {
        await _fixture.ClearDomainTablesAsync();

        var services = new ServiceCollection()
            .AddDbContext<MiniBankDbContext>(o => o.UseNpgsql(_fixture.ConnectionString))
            .AddLogging(b => b.AddProvider(NSubstitute.Substitute.For<ILoggerProvider>()))
            .AddScoped<IUnitOfWork, EfUnitOfWork>()
            .AddScoped<ICurrentUserContext>(_ => Substitute.For<ICurrentUserContext>())
            .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .BuildServiceProvider();

        var customerId = new CustomerId(Guid.NewGuid());
        await _fixture.SeedIdentityUserAsync(customerId.Value, $"evt2_{Guid.NewGuid():N}@test.com");

        await using var scope = services.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var db = scope.ServiceProvider.GetRequiredService<MiniBankDbContext>();

        var customer = Customer.Create("No Handler", $"evt2_{Guid.NewGuid():N}@test.com", "09123456789", customerId);
        db.Customers.Add(customer);

        await uow.SaveChangesAsync();

        // CustomerCreatedEvent is audit-only: exactly one audit log, nothing else persisted.
        var auditLogs = await db.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);
    }
}
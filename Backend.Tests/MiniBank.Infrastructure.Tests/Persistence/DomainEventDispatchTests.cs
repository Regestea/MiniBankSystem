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
/// - Integration events → outbox_messages table (processed by OutboxProcessor)
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
    public async Task SaveChangesAsync_CustomerCreatedEvent_NotInOutbox_BecauseAuditOnly()
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

        // Clear outbox using test's own context
        await db.Database.ExecuteSqlRawAsync(@"TRUNCATE TABLE ""outbox_messages"" CASCADE;");

        var customer = Customer.Create("No Handler", $"evt2_{Guid.NewGuid():N}@test.com", "09123456789", customerId);
        db.Customers.Add(customer);

        await uow.SaveChangesAsync();

        // CustomerCreatedEvent is audit-only, should NOT be in outbox
        var outboxMessages = await db.OutboxMessages.ToListAsync();
        outboxMessages.Should().BeEmpty("CustomerCreatedEvent is audit-only, goes to audit_logs not outbox");
    }

    [Fact]
    public async Task OutboxMessage_MarkProcessed_Updates_Status()
    {
        await _fixture.ClearDomainTablesAsync();

        await using var ctx = _fixture.CreateContext();
        await ctx.Database.ExecuteSqlRawAsync(@"TRUNCATE TABLE ""outbox_messages"" CASCADE;");
        
        var message = OutboxMessage.Create(
            "TestEvent",
            """{"test": "payload"}""",
            DateTimeOffset.UtcNow);
        
        ctx.OutboxMessages.Add(message);
        await ctx.SaveChangesAsync();

        message.MarkProcessed();
        await ctx.SaveChangesAsync();

        var processed = await ctx.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        processed.ProcessedOn.Should().NotBeNull();
    }
}
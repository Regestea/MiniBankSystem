using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.Events;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Infrastructure.Persistence;
using MiniBank.Infrastructure.Tests.Fixtures;

namespace MiniBank.Infrastructure.Tests.Persistence;

/// <summary>
/// Verifies EfUnitOfWork dispatches collected domain events to registered
/// IDomainEventHandler implementations after SaveChangesAsync, and clears them.
/// </summary>
[Collection("postgres")]
public sealed class DomainEventDispatchTests
{
    private readonly PostgresFixture _fixture;
    public DomainEventDispatchTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SaveChangesAsync_Dispatches_And_Clears_DomainEvents()
    {
        await _fixture.ClearDomainTablesAsync();

        var captured = new List<CustomerCreatedEvent>();
        var services = new ServiceCollection()
            .AddDbContext<MiniBankDbContext>(o => o.UseNpgsql(_fixture.ConnectionString))
            .AddScoped<IUnitOfWork, EfUnitOfWork>()
            .AddScoped<IDomainEventHandler<CustomerCreatedEvent>>(_ => new RecordingHandler(captured))
            .BuildServiceProvider();

        var customerId = new CustomerId(Guid.NewGuid());
        await _fixture.SeedIdentityUserAsync(customerId.Value, $"evt_{Guid.NewGuid():N}@test.com");

        await using var scope = services.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var db = scope.ServiceProvider.GetRequiredService<MiniBankDbContext>();

        var customer = Customer.Create("Event User", $"evt_{Guid.NewGuid():N}@test.com", "09123456789", customerId);
        db.Customers.Add(customer);
        customer.DomainEvents.Should().HaveCount(1, "Customer.Create raises CustomerCreatedEvent");

        await uow.SaveChangesAsync();

        captured.Should().HaveCount(1);
        captured[0].CustomerId.Value.Should().Be(customerId.Value);
        customer.DomainEvents.Should().BeEmpty("events must be cleared after dispatch");
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutHandlers_DoesNotThrow_And_ClearsEvents()
    {
        await _fixture.ClearDomainTablesAsync();

        var services = new ServiceCollection()
            .AddDbContext<MiniBankDbContext>(o => o.UseNpgsql(_fixture.ConnectionString))
            .AddScoped<IUnitOfWork, EfUnitOfWork>()
            .BuildServiceProvider();

        var customerId = new CustomerId(Guid.NewGuid());
        await _fixture.SeedIdentityUserAsync(customerId.Value, $"evt2_{Guid.NewGuid():N}@test.com");

        await using var scope = services.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var db = scope.ServiceProvider.GetRequiredService<MiniBankDbContext>();

        var customer = Customer.Create("No Handler", $"evt2_{Guid.NewGuid():N}@test.com", "09123456789", customerId);
        db.Customers.Add(customer);

        var act = async () => await uow.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        customer.DomainEvents.Should().BeEmpty();
    }

    private sealed class RecordingHandler(List<CustomerCreatedEvent> sink) : IDomainEventHandler<CustomerCreatedEvent>
    {
        public Task HandleAsync(CustomerCreatedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            sink.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}

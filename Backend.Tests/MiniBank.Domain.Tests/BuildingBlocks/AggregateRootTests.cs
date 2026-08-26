using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Domain.Tests.BuildingBlocks;

// Dummy aggregate for testing base behavior
internal sealed class TestAggregate : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;

    public TestAggregate(Guid id, string name) : base(id)
    {
        Name = name;
    }

    public void DoSomething()
    {
        AddDomainEvent(new TestEvent(Id));
        IncrementVersion();
    }

    public void Clear() => ClearDomainEvents();
}

internal sealed record TestEvent(Guid AggregateId) : DomainEvent;

public class AggregateRootTests
{
    [Fact]
    public void AggregateRoot_InitialVersion_IsZero()
    {
        var agg = new TestAggregate(Guid.NewGuid(), "test");
        Assert.Equal(0, agg.Version);
        Assert.Empty(agg.DomainEvents);
    }

    [Fact]
    public void AddDomainEvent_AddsToCollection()
    {
        var agg = new TestAggregate(Guid.NewGuid(), "test");
        agg.DoSomething();
        Assert.Single(agg.DomainEvents);
        Assert.IsType<TestEvent>(agg.DomainEvents.First());
    }

    [Fact]
    public void IncrementVersion_IncrementsVersionAndUpdatesTimestamp()
    {
        var agg = new TestAggregate(Guid.NewGuid(), "test");
        var before = agg.UpdatedAt;
        // slight delay to ensure timestamp difference
        Thread.Sleep(10);
        agg.DoSomething();
        Assert.Equal(1, agg.Version);
        Assert.True(agg.UpdatedAt > before);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var agg = new TestAggregate(Guid.NewGuid(), "test");
        agg.DoSomething();
        agg.DoSomething();
        Assert.Equal(2, agg.DomainEvents.Count);
        agg.Clear();
        Assert.Empty(agg.DomainEvents);
    }

    [Fact]
    public void MultipleIncrements_VersionIncreasesCorrectly()
    {
        var agg = new TestAggregate(Guid.NewGuid(), "test");
        agg.DoSomething();
        agg.DoSomething();
        agg.DoSomething();
        Assert.Equal(3, agg.Version);
    }

    [Fact]
    public void CreatedAt_IsSetOnConstruction()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var agg = new TestAggregate(Guid.NewGuid(), "test");
        Assert.True(agg.CreatedAt >= before);
        Assert.True(agg.UpdatedAt >= before);
    }

    [Fact]
    public void RealAggregate_Customer_UsesBuildingBlocksCorrectly()
    {
        var customer = Customer.Create(new FullName("John Doe"), new Email("john@doe.com"), new PhoneNumber("1234567890"));
        Assert.Equal(0, customer.Version);
        Assert.Single(customer.DomainEvents);
        customer.Verify();
        Assert.Equal(1, customer.Version);
        Assert.Equal(2, customer.DomainEvents.Count);
    }

    [Fact]
    public void RealAggregate_Account_UsesBuildingBlocksCorrectly()
    {
        var account = Account.Open(new CustomerId(Guid.NewGuid()), AccountType.Current);
        Assert.Equal(0, account.Version);
        account.Deposit(Money.FromDecimal(100m));
        Assert.Equal(1, account.Version);
        Assert.Equal(2, account.DomainEvents.Count); // Opened + Deposited
    }
}

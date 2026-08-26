using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate.Events;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Domain.Tests.BuildingBlocks;

public class DomainEventTests
{
    private sealed record TestEvent(Guid Id) : DomainEvent;

    [Fact]
    public void DomainEvent_HasUniqueIdAndOccurredOn()
    {
        var e1 = new TestEvent(Guid.NewGuid());
        var e2 = new TestEvent(Guid.NewGuid());
        Assert.NotEqual(e1.EventId, e2.EventId);
        Assert.True((DateTimeOffset.UtcNow - e1.OccurredOn).TotalSeconds < 5);
        Assert.True((DateTimeOffset.UtcNow - e2.OccurredOn).TotalSeconds < 5);
    }

    [Fact]
    public void DomainEvent_ImplementsIDomainEvent()
    {
        var e = new TestEvent(Guid.NewGuid());
        Assert.IsAssignableFrom<IDomainEvent>(e);
        Assert.NotEqual(Guid.Empty, e.EventId);
        Assert.NotEqual(default, e.OccurredOn);
    }

    [Fact]
    public void CustomerCreatedEvent_Creation_SetsProperties()
    {
        var id = new CustomerId(Guid.NewGuid());
        var fullName = new FullName("John Doe");
        var email = new Email("john@doe.com");
        var phone = new PhoneNumber("1234567890");
        var ev = new CustomerCreatedEvent(id, fullName, email, phone);
        Assert.Equal(id, ev.CustomerId);
        Assert.Equal(fullName, ev.FullName);
        Assert.Equal(email, ev.Email);
        Assert.Equal(phone, ev.PhoneNumber);
        Assert.NotEqual(Guid.Empty, ev.EventId);
    }

    [Fact]
    public void DomainEvent_RecordEquality_ByValue()
    {
        var id = Guid.NewGuid();
        var e1 = new TestEvent(id);
        var e2 = e1 with { }; // same EventId and OccurredOn via copy
        // Since DomainEvent is record, equality includes EventId and OccurredOn
        Assert.Equal(e1.EventId, e2.EventId);
    }
}

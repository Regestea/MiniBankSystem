using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.Events;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Domain.Tests.CustomerAggregate;

public class CustomerTests
{
    private static Customer CreateValidCustomer()
    {
        var fullName = new FullName("Amir Hossein");
        var email = new Email("amir@example.com");
        var phone = new PhoneNumber("09123456789");
        return Customer.Create(fullName, email, phone);
    }

    [Fact]
    public void Create_ValidData_CreatesPendingCustomerWithEvent()
    {
        var customer = CreateValidCustomer();
        Assert.Equal(CustomerStatus.Pending, customer.Status);
        Assert.NotEqual(Guid.Empty, (Guid)customer.Id);
        Assert.Single(customer.DomainEvents);
        Assert.IsType<CustomerCreatedEvent>(customer.DomainEvents.First());
        Assert.Equal(0, customer.Version);
    }

    [Fact]
    public void Create_WithSpecificId_UsesProvidedId()
    {
        var id = new CustomerId(Guid.NewGuid());
        var customer = Customer.Create(new FullName("John Doe"), new Email("john@doe.com"), new PhoneNumber("1234567890"), id);
        Assert.Equal(id, customer.Id);
    }

    [Fact]
    public void Verify_FromPending_SucceedsAndRaisesEvent()
    {
        var customer = CreateValidCustomer();
        customer.ClearDomainEvents();
        customer.Verify();
        Assert.Equal(CustomerStatus.Verified, customer.Status);
        Assert.Equal(1, customer.Version);
        Assert.Single(customer.DomainEvents);
        Assert.IsType<CustomerVerifiedEvent>(customer.DomainEvents.First());
    }

    [Fact]
    public void Verify_FromVerified_ThrowsDomainOperationNotAllowed()
    {
        var customer = CreateValidCustomer();
        customer.Verify();
        var ex = Assert.Throws<DomainOperationNotAllowedException>(() => customer.Verify());
        Assert.Contains("already verified", ex.Details.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Verify_FromBlocked_ThrowsDomainOperationNotAllowed()
    {
        var customer = CreateValidCustomer();
        customer.Block();
        var ex = Assert.Throws<DomainOperationNotAllowedException>(() => customer.Verify());
        Assert.Contains("Blocked", ex.Details.ToString());
    }

    [Fact]
    public void Block_FromPending_Succeeds()
    {
        var customer = CreateValidCustomer();
        customer.ClearDomainEvents();
        customer.Block();
        Assert.Equal(CustomerStatus.Blocked, customer.Status);
        Assert.Equal(1, customer.Version);
        Assert.IsType<CustomerBlockedEvent>(customer.DomainEvents.First());
    }

    [Fact]
    public void Block_FromVerified_Succeeds()
    {
        var customer = CreateValidCustomer();
        customer.Verify();
        customer.ClearDomainEvents();
        customer.Block();
        Assert.Equal(CustomerStatus.Blocked, customer.Status);
    }

    [Fact]
    public void Block_AlreadyBlocked_Throws()
    {
        var customer = CreateValidCustomer();
        customer.Block();
        Assert.Throws<DomainOperationNotAllowedException>(() => customer.Block());
    }

    [Fact]
    public void UpdateInformation_WhenBlocked_Throws()
    {
        var customer = CreateValidCustomer();
        customer.Block();
        Assert.Throws<DomainOperationNotAllowedException>(() =>
            customer.UpdateInformation(new FullName("Jane Doe"), new Email("jane@doe.com"), new PhoneNumber("0987654321")));
    }

    [Fact]
    public void UpdateInformation_Valid_UpdatesAndIncrementsVersion()
    {
        var customer = CreateValidCustomer();
        customer.Verify();
        var initialVersion = customer.Version;
        var newName = new FullName("Jane Smith");
        var newEmail = new Email("jane.smith@example.com");
        var newPhone = new PhoneNumber("0987654321");
        customer.UpdateInformation(newName, newEmail, newPhone);
        Assert.Equal(newName, customer.FullName);
        Assert.Equal(newEmail, customer.Email);
        Assert.Equal(newPhone, customer.PhoneNumber);
        Assert.Equal(initialVersion + 1, customer.Version);
    }

    [Fact]
    public void UpdateInformation_NullArgs_ThrowsDomainValidation()
    {
        var customer = CreateValidCustomer();
        Assert.Throws<DomainValidationException>(() => customer.UpdateInformation(null!, new Email("a@b.c"), new PhoneNumber("1234567890")));
        Assert.Throws<DomainValidationException>(() => customer.UpdateInformation(new FullName("John Doe"), null!, new PhoneNumber("1234567890")));
        Assert.Throws<DomainValidationException>(() => customer.UpdateInformation(new FullName("John Doe"), new Email("a@b.c"), null!));
    }

    [Fact]
    public void ClearDomainEvents_ClearsEvents()
    {
        var customer = CreateValidCustomer();
        Assert.NotEmpty(customer.DomainEvents);
        customer.ClearDomainEvents();
        Assert.Empty(customer.DomainEvents);
    }

    [Fact]
    public void DomainEvents_CreatedAtIsSet()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var customer = CreateValidCustomer();
        Assert.True(customer.CreatedAt >= before);
        Assert.True(customer.UpdatedAt >= before);
    }

    [Fact]
    public void UpdateInformation_RaisesCustomerUpdatedEvent()
    {
        var customer = CreateValidCustomer();
        customer.ClearDomainEvents();

        var newName = new FullName("Jane Smith");
        var newEmail = new Email("jane@example.com");
        var newPhone = new PhoneNumber("09120000000");
        customer.UpdateInformation(newName, newEmail, newPhone);

        Assert.Single(customer.DomainEvents);
        var evt = Assert.IsType<CustomerUpdatedEvent>(customer.DomainEvents.First());
        Assert.Equal(customer.Id, evt.CustomerId);
        Assert.Equal(newName, evt.FullName);
        Assert.Equal(newEmail, evt.Email);
        Assert.Equal(newPhone, evt.PhoneNumber);
    }

    [Fact]
    public void Rehydrate_RestoresStateWithoutEventsOrTransitions()
    {
        var id = new CustomerId(Guid.NewGuid());
        var fullName = new FullName("Rehydrated Person");
        var email = new Email("rehydrated@person.com");
        var phone = new PhoneNumber("09121112233");
        var createdAt = DateTimeOffset.UtcNow.AddDays(-10);
        var updatedAt = DateTimeOffset.UtcNow.AddDays(-1);

        var customer = Customer.Rehydrate(id, fullName, email, phone, CustomerStatus.Verified, 7, createdAt, updatedAt);

        Assert.Equal(id, customer.Id);
        Assert.Equal(fullName, customer.FullName);
        Assert.Equal(email, customer.Email);
        Assert.Equal(phone, customer.PhoneNumber);
        Assert.Equal(CustomerStatus.Verified, customer.Status);
        Assert.Equal(7, customer.Version);
        Assert.Equal(createdAt, customer.CreatedAt);
        Assert.Equal(updatedAt, customer.UpdatedAt);
        // Rehydration must not raise events
        Assert.Empty(customer.DomainEvents);
    }
}

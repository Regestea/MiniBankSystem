using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate.Events;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Domain.CustomerAggregate;

public sealed class Customer : AggregateRoot<CustomerId>
{
    public FullName FullName { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public PhoneNumber PhoneNumber { get; private set; } = null!;
    public CustomerStatus Status { get; private set; }

    private Customer() { }

    private Customer(CustomerId id, FullName fullName, Email email, PhoneNumber phoneNumber)
        : base(id)
    {
        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        Status = CustomerStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new CustomerCreatedEvent(id, fullName, email, phoneNumber));
    }

    public static Customer Create(FullName fullName, Email email, PhoneNumber phoneNumber, CustomerId? id = null)
    {
        id ??= new CustomerId(Guid.NewGuid());
        return new Customer(id, fullName, email, phoneNumber);
    }

    public void Verify()
    {
        if (Status == CustomerStatus.Verified)
            throw new DomainOperationNotAllowedException(nameof(Status), "Customer already verified.");

        if (Status == CustomerStatus.Blocked)
            throw new DomainOperationNotAllowedException(nameof(Status), "Blocked customer cannot be verified.");

        if (Status != CustomerStatus.Pending)
            throw new DomainOperationNotAllowedException(nameof(Status), $"Cannot verify customer in status {Status}.");

        Status = CustomerStatus.Verified;
        IncrementVersion();
        AddDomainEvent(new CustomerVerifiedEvent(Id));
    }

    public void Block()
    {
        if (Status == CustomerStatus.Blocked)
            throw new DomainOperationNotAllowedException(nameof(Status), "Customer already blocked.");

        Status = CustomerStatus.Blocked;
        IncrementVersion();
        AddDomainEvent(new CustomerBlockedEvent(Id));
    }

    public void UpdateInformation(FullName fullName, Email email, PhoneNumber phoneNumber)
    {
        if (Status == CustomerStatus.Blocked)
            throw new DomainOperationNotAllowedException(nameof(Status), "Blocked customer cannot be updated.");

        FullName = fullName ?? throw new DomainValidationException(nameof(fullName), "FullName cannot be null.");
        Email = email ?? throw new DomainValidationException(nameof(email), "Email cannot be null.");
        PhoneNumber = phoneNumber ?? throw new DomainValidationException(nameof(phoneNumber), "PhoneNumber cannot be null.");

        IncrementVersion();
        AddDomainEvent(new CustomerUpdatedEvent(Id, FullName, Email, PhoneNumber));
    }

    // For rehydration / EF Core
    private Customer(CustomerId id, FullName fullName, Email email, PhoneNumber phoneNumber, CustomerStatus status, int version, DateTimeOffset createdAt, DateTimeOffset updatedAt)
        : base(id)
    {
        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        Status = status;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Rehydrates a persisted Customer without raising events or enforcing transitions.</summary>
    public static Customer Rehydrate(
        CustomerId id,
        FullName fullName,
        Email email,
        PhoneNumber phoneNumber,
        CustomerStatus status,
        int version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => new(id, fullName, email, phoneNumber, status, version, createdAt, updatedAt);
}

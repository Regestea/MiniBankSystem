using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Abstractions;
using MiniBank.Features.Customers.CreateCustomer;

namespace MiniBank.Domain.Tests.Features;

public class CreateCustomerHandlerTests
{
    private readonly FakeCustomerRepository _customers = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly CreateCustomerHandler _handler;

    public CreateCustomerHandlerTests()
        => _handler = new CreateCustomerHandler(_customers, _uow);

    [Fact]
    public async Task Handle_ValidCommand_CreatesPendingCustomer()
    {
        var command = new CreateCustomerCommand("Amir Hossein", "amir@test.com", "09123456789");

        var response = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, response.CustomerId);
        Assert.Equal("Pending", response.Status);
        Assert.Single(_customers.Store);
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflict()
    {
        await _handler.HandleAsync(new CreateCustomerCommand("First Person", "dup@test.com", "09123456789"));

        var ex = await Assert.ThrowsAsync<DomainConflictException>(
            () => _handler.HandleAsync(new CreateCustomerCommand("Second Person", "DUP@test.com", "09876543210")));

        Assert.Contains("already registered", ex.Details.ToString());
    }

    [Fact]
    public void Validator_RejectsInvalidPhone()
    {
        var validator = new CreateCustomerValidator();
        var result = validator.Validate(new CreateCustomerCommand("John", "john@x.com", "123")); // too short

        Assert.False(result.IsValid);
    }
}

// ICurrentUserContext fake — shared by account handler tests
internal sealed class FakeCurrentUserContext(Guid? customerId) : ICurrentUserContext
{
    public Guid UserId { get; } = Guid.NewGuid();

    public Task<Guid?> GetCustomerIdAsync(CancellationToken ct = default)
        => Task.FromResult(customerId);
}

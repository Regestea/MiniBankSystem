using FluentAssertions;
using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Features.Customers.RegisterCustomer;
using MiniBank.Features.Messaging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MiniBank.Features.Tests.Customers.LinkCustomer;

public sealed class RegisterCustomerHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IIdentityUserService _identity = Substitute.For<IIdentityUserService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private RegisterCustomerHandler CreateHandler() => new(_customers, _identity, _uow);

    [Fact]
    public async Task HandleAsync_Valid_CreatesCustomer_WithSameGuid()
    {
        _customers.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _identity.CreateUserAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _identity.EnsureUserRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var response = await handler.HandleAsync(
            new RegisterCustomerCommand("user@test.com", "P@ssw0rd1", "John Doe", "09123456789"));

        response.Email.Should().Be("user@test.com");
        response.FullName.Should().Be("John Doe");

        await _identity.Received(1).CreateUserAsync(
            Arg.Is<Guid>(id => id != Guid.Empty),
            Arg.Is<string>(e => e == "user@test.com"),
            Arg.Is<string>(p => p == "P@ssw0rd1"),
            Arg.Any<CancellationToken>());

        await _customers.Received(1).AddAsync(
            Arg.Is<Customer>(c => c.FullName == "John Doe" && c.Email == "user@test.com"),
            Arg.Any<CancellationToken>());

        await _identity.Received(1).EnsureUserRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ThrowsConflict()
    {
        _customers.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(
            new RegisterCustomerCommand("dup@test.com", "P@ssw0rd1", "John", "09123456789"));

        await act.Should().ThrowAsync<DomainConflictException>();
        await _identity.DidNotReceive().CreateUserAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _customers.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidEmail_ThrowsValidation()
    {
        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(
            new RegisterCustomerCommand("bad-email", "P@ssw0rd1", "John", "09123456789"));

        await act.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task HandleAsync_IdentityCreateFails_ThrowsInvalidOperation()
    {
        _customers.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _identity.CreateUserAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Failed to create user."));

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(
            new RegisterCustomerCommand("user@test.com", "P@ssw0rd1", "John", "09123456789"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _customers.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SharedGuid_IdentityAndCustomerHaveSameId()
    {
        Guid? capturedId = null;
        _customers.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _identity.CreateUserAsync(Arg.Do<Guid>(id => capturedId = id), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _identity.EnsureUserRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        Customer? capturedCustomer = null;
        _customers.AddAsync(Arg.Do<Customer>(c => capturedCustomer = c), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var response = await handler.HandleAsync(
            new RegisterCustomerCommand("user@test.com", "P@ssw0rd1", "John", "09123456789"));

        capturedId.Should().NotBeNull();
        capturedCustomer.Should().NotBeNull();
        capturedCustomer!.Id.Value.Should().Be(capturedId!.Value);
        response.CustomerId.Should().Be(capturedId.Value);
    }
}

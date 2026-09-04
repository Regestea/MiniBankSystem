using FluentAssertions;
using Microsoft.Extensions.Logging;
using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Domain.RiskAggregate;
using MiniBank.Features.Customers.RegisterCustomer;
using MiniBank.Features.Messaging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MiniBank.Features.Tests.Customers.LinkCustomer;

public sealed class RegisterCustomerHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IRiskRepository _riskRepo = Substitute.For<IRiskRepository>();
    private readonly IIdentityUserService _identity = Substitute.For<IIdentityUserService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ILogger<RegisterCustomerHandler> _logger = Substitute.For<ILogger<RegisterCustomerHandler>>();

    private RegisterCustomerHandler CreateHandler() => new(_customers, _riskRepo, _identity, _uow, _logger);

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
    public async Task HandleAsync_IdentityDuplicateEmail_ThrowsConflict()
    {
        _customers.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _identity.CreateUserAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DomainConflictException("email", "Email already registered."));

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(
            new RegisterCustomerCommand("dup@identity.com", "P@ssw0rd1", "John", "09123456789"));

        await act.Should().ThrowAsync<DomainConflictException>();
        await _customers.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_IdentityCreateFails_ThrowsDomainException()
    {
        _customers.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _identity.CreateUserAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DomainOperationNotAllowedException("user", "Password policy violated."));

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(
            new RegisterCustomerCommand("user@test.com", "P@ssw0rd1", "John", "09123456789"));

        await act.Should().ThrowAsync<DomainOperationNotAllowedException>();
        await _customers.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RoleAssignment_Happens_After_AtomicSave()
    {
        _customers.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _identity.CreateUserAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _identity.EnsureUserRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        await handler.HandleAsync(
            new RegisterCustomerCommand("user@test.com", "P@ssw0rd1", "John", "09123456789"));

        Received.InOrder(async () =>
        {
            await _identity.CreateUserAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            await _uow.SaveChangesAsync(Arg.Any<CancellationToken>());
            await _identity.EnsureUserRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        });
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

    [Fact]
    public async Task HandleAsync_DomainSaveFails_CompensatesOrphanIdentityUser()
    {
        _customers.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _identity.CreateUserAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DomainConflictException("customer", "Simulated save failure."));

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(
            new RegisterCustomerCommand("user@test.com", "P@ssw0rd1", "John", "09123456789"));

        await act.Should().ThrowAsync<DomainConflictException>();
        await _identity.Received(1).DeleteUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}

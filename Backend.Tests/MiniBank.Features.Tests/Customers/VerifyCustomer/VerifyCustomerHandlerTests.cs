using FluentAssertions;
using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Features.Customers.VerifyCustomer;
using NSubstitute;

namespace MiniBank.Features.Tests.Customers.VerifyCustomer;

public sealed class VerifyCustomerHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();

    private VerifyCustomerHandler CreateHandler()
    {
        _currentUser.IsAdmin.Returns(true);
        return new(_customers, _currentUser, _uow);
    }

    [Fact]
    public async Task HandleAsync_PendingCustomer_Verifies_And_Persists()
    {
        var customer = Customer.Create("John Doe", "john@test.com", "09123456789");
        var handler = CreateHandler();
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var response = await handler.HandleAsync(new VerifyCustomerCommand(customer.Id.Value));

        response.Status.Should().Be("Verified");
        response.Version.Should().Be(1);
        customer.Status.Should().Be(CustomerStatus.Verified);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CustomerNotFound_ThrowsNotFound()
    {
        var handler = CreateHandler();
        _customers.GetByIdAsync(Arg.Any<Domain.CustomerAggregate.ValueObjects.CustomerId>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var act = async () => await handler.HandleAsync(new VerifyCustomerCommand(Guid.NewGuid()));

        await act.Should().ThrowAsync<NotFoundException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NonAdmin_ThrowsForbidden()
    {
        var customer = Customer.Create("John Doe", "john@test.com", "09123456789");
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _currentUser.IsAdmin.Returns(false);
        var handler = new VerifyCustomerHandler(_customers, _currentUser, _uow);

        var act = async () => await handler.HandleAsync(new VerifyCustomerCommand(customer.Id.Value));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task HandleAsync_AlreadyVerified_ThrowsOperationNotAllowed()
    {
        var customer = Customer.Create("Jane", "jane@test.com", "09123456789");
        customer.Verify();
        var handler = CreateHandler();
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var act = async () => await handler.HandleAsync(new VerifyCustomerCommand(customer.Id.Value));

        await act.Should().ThrowAsync<DomainOperationNotAllowedException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_BlockedCustomer_ThrowsOperationNotAllowed()
    {
        var customer = Customer.Create("Blocked", "blocked@test.com", "09123456789");
        customer.Block();
        var handler = CreateHandler();
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var act = async () => await handler.HandleAsync(new VerifyCustomerCommand(customer.Id.Value));

        await act.Should().ThrowAsync<DomainOperationNotAllowedException>();
    }
}

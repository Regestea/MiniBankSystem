using FluentAssertions;
using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Features.Customers.BlockCustomer;
using NSubstitute;

namespace MiniBank.Features.Tests.Customers.BlockCustomer;

public sealed class BlockCustomerHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();

    private BlockCustomerHandler CreateHandler()
    {
        _currentUser.IsAdmin.Returns(true);
        return new(_customers, _currentUser, _uow);
    }

    [Fact]
    public async Task HandleAsync_PendingCustomer_Blocks()
    {
        var customer = Customer.Create("John", "john@test.com", "09123456789");
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        var handler = CreateHandler();

        var response = await handler.HandleAsync(new BlockCustomerCommand(customer.Id.Value));

        response.Status.Should().Be("Blocked");
        customer.Status.Should().Be(CustomerStatus.Blocked);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_VerifiedCustomer_Blocks()
    {
        var customer = Customer.Create("Jane", "jane@test.com", "09123456789");
        customer.Verify();
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        var handler = CreateHandler();

        var response = await handler.HandleAsync(new BlockCustomerCommand(customer.Id.Value));

        response.Status.Should().Be("Blocked");
    }

    [Fact]
    public async Task HandleAsync_CustomerNotFound_ThrowsNotFound()
    {
        _customers.GetByIdAsync(Arg.Any<Domain.CustomerAggregate.ValueObjects.CustomerId>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new BlockCustomerCommand(Guid.NewGuid()));

        await act.Should().ThrowAsync<NotFoundException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyBlocked_ThrowsOperationNotAllowed()
    {
        var customer = Customer.Create("Bob", "bob@test.com", "09123456789");
        customer.Block();
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new BlockCustomerCommand(customer.Id.Value));

        await act.Should().ThrowAsync<DomainOperationNotAllowedException>()
            .Where(ex => ex.Details.ToString()!.Contains("already blocked"));
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

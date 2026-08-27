using FluentAssertions;
using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Features.Accounts.OpenAccount;
using NSubstitute;

namespace MiniBank.Features.Tests.Accounts.OpenAccount;

public sealed class OpenAccountHandlerTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private OpenAccountHandler CreateHandler() => new(_accounts, _customers, _currentUser, _uow);

    [Fact]
    public async Task HandleAsync_VerifiedCustomer_CreatesAccount()
    {
        var customer = Customer.Create("John", "john@test.com", "09123456789");
        customer.Verify();
        var customerId = customer.Id.Value;
        _currentUser.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customerId);
        _customers.GetByIdAsync(Arg.Is<Domain.CustomerAggregate.ValueObjects.CustomerId>(id => id.Value == customerId), Arg.Any<CancellationToken>()).Returns(customer);

        var handler = CreateHandler();
        var response = await handler.HandleAsync(new OpenAccountCommand(Guid.NewGuid(), "Current"));

        response.AccountId.Should().NotBe(Guid.Empty);
        response.Status.Should().Be("Active");
        await _accounts.Received(1).AddAsync(Arg.Is<Account>(a => a.CustomerId.Value == customerId), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UserWithoutCustomer_ThrowsForbidden()
    {
        _currentUser.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new OpenAccountCommand(Guid.NewGuid(), "Current"));

        await act.Should().ThrowAsync<ForbiddenException>()
            .Where(ex => ex.Field == "customer");
        await _accounts.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CustomerNotFound_ThrowsNotFound()
    {
        var cid = Guid.NewGuid();
        _currentUser.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(cid);
        _customers.GetByIdAsync(Arg.Any<Domain.CustomerAggregate.ValueObjects.CustomerId>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new OpenAccountCommand(Guid.NewGuid(), "Savings"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_PendingCustomer_ThrowsOperationNotAllowed()
    {
        var customer = Customer.Create("Pending", "pending@test.com", "09123456789");
        // status is Pending
        _currentUser.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customer.Id.Value);
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new OpenAccountCommand(Guid.NewGuid(), "Current"));

        await act.Should().ThrowAsync<DomainOperationNotAllowedException>()
            .Where(ex => ex.Details.ToString()!.Contains("verified"));
    }

    [Theory]
    [InlineData("Savings", AccountType.Savings)]
    [InlineData("Current", AccountType.Current)]
    [InlineData("Other", AccountType.Current)]
    public async Task HandleAsync_MapsAccountType(string input, AccountType expected)
    {
        var customer = Customer.Create("John", "john@test.com", "09123456789");
        customer.Verify();
        _currentUser.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customer.Id.Value);
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        Account? captured = null;
        _accounts.AddAsync(Arg.Do<Account>(a => captured = a), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var handler = CreateHandler();
        await handler.HandleAsync(new OpenAccountCommand(Guid.NewGuid(), input));

        captured!.AccountType.Should().Be(expected);
    }
}

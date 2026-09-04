using FluentAssertions;
using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
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
        var userId = Guid.NewGuid();
        var customer = Customer.Create(new FullName("John"), new Email("john@test.com"), new PhoneNumber("09123456789"), new CustomerId(userId));
        customer.Verify();
        _currentUser.UserId.Returns(userId);
        _customers.GetByIdAsync(Arg.Is<Domain.CustomerAggregate.ValueObjects.CustomerId>(id => id.Value == userId), Arg.Any<CancellationToken>()).Returns(customer);

        var handler = CreateHandler();
        var response = await handler.HandleAsync(new OpenAccountCommand("Current"));

        response.AccountId.Should().NotBe(Guid.Empty);
        response.Status.Should().Be("PendingApproval");
        await _accounts.Received(1).AddAsync(Arg.Is<Account>(a => a.CustomerId.Value == userId), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CustomerNotFound_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _customers.GetByIdAsync(Arg.Any<Domain.CustomerAggregate.ValueObjects.CustomerId>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new OpenAccountCommand("Savings"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_PendingCustomer_ThrowsOperationNotAllowed()
    {
        var userId = Guid.NewGuid();
        var customer = Customer.Create(new FullName("Pending"), new Email("pending@test.com"), new PhoneNumber("09123456789"), new CustomerId(userId));
        // status is Pending
        _currentUser.UserId.Returns(userId);
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new OpenAccountCommand("Current"));

        await act.Should().ThrowAsync<DomainOperationNotAllowedException>()
            .Where(ex => ex.Details.ToString()!.Contains("verified"));
    }

    [Theory]
    [InlineData("Savings", AccountType.Savings)]
    [InlineData("Current", AccountType.Current)]
    public async Task HandleAsync_MapsAccountType(string input, AccountType expected)
    {
        var userId = Guid.NewGuid();
        var customer = Customer.Create(new FullName("John"), new Email("john@test.com"), new PhoneNumber("09123456789"), new CustomerId(userId));
        customer.Verify();
        _currentUser.UserId.Returns(userId);
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        Account? captured = null;
        _accounts.AddAsync(Arg.Do<Account>(a => captured = a), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var handler = CreateHandler();
        await handler.HandleAsync(new OpenAccountCommand(input));

        captured!.AccountType.Should().Be(expected);
    }

    [Fact]
    public async Task HandleAsync_InvalidAccountType_ThrowsValidation()
    {
        var userId = Guid.NewGuid();
        var customer = Customer.Create(new FullName("John"), new Email("john@test.com"), new PhoneNumber("09123456789"), new CustomerId(userId));
        customer.Verify();
        _currentUser.UserId.Returns(userId);
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new OpenAccountCommand("Other"));

        await act.Should().ThrowAsync<DomainValidationException>();
    }
}
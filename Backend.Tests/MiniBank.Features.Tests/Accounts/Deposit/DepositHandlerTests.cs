using FluentAssertions;
using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Features.Accounts.Deposit;
using MiniBank.Features.Customers;
using NSubstitute;

namespace MiniBank.Features.Tests.Accounts.Deposit;

public sealed class DepositHandlerTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly ITransactionRepository _transactions = Substitute.For<ITransactionRepository>();
    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly ICustomerAccessGuard _customerAccess = Substitute.For<ICustomerAccessGuard>();
    private DepositHandler CreateHandler() => new(_accounts, _customerAccess, _transactions, _currentUser, _uow);

    private static Account CreateOwnedAccount(Guid customerId) =>
        Account.Open(new CustomerId(customerId), AccountType.Current);

    [Fact]
    public async Task HandleAsync_OwnedAccount_CreatesTransactionAndPersists()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateOwnedAccount(ownerId);
        _accounts.LoadAsync(new AccountId(account.Id.Value), Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.UserId.Returns(ownerId);

        var handler = CreateHandler();
        var response = await handler.HandleAsync(new DepositCommand(account.Id.Value, 500m));

        response.Amount.Should().Be(500m);
        response.Type.Should().Be("Deposit");
        account.Balance.Amount.Should().Be(500m);
        await _transactions.Received(1).AddAsync(Arg.Is<Transaction>(t => t.Amount.Amount == 500m), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AccountNotFound_ThrowsNotFound()
    {
        _accounts.LoadAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns((Account?)null);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new DepositCommand(Guid.NewGuid(), 100m));

        await act.Should().ThrowAsync<NotFoundException>();
        await _transactions.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NotOwned_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateOwnedAccount(ownerId);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.UserId.Returns(Guid.NewGuid()); // different user

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new DepositCommand(account.Id.Value, 100m));

        await act.Should().ThrowAsync<ForbiddenException>();
        await _transactions.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FrozenAccount_ThrowsOperationNotAllowed()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateOwnedAccount(ownerId);
        account.Freeze();
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.UserId.Returns(ownerId);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new DepositCommand(account.Id.Value, 100m));

        await act.Should().ThrowAsync<DomainOperationNotAllowedException>();
        await _transactions.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_BlockedCustomer_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateOwnedAccount(ownerId);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.UserId.Returns(ownerId);
        _currentUser.IsAdmin.Returns(false);

        var customers = Substitute.For<MiniBank.Domain.CustomerAggregate.ICustomerRepository>();
        var blocked = MiniBank.Domain.CustomerAggregate.Customer.Create(
            "Blocked User", "blocked@test.com", "09123456789",
            new MiniBank.Domain.CustomerAggregate.ValueObjects.CustomerId(ownerId));
        blocked.Block();
        customers.GetByIdAsync(Arg.Any<CustomerId>(), Arg.Any<CancellationToken>()).Returns(blocked);

        var guard = new MiniBank.Features.CustomerAccessGuard(customers, _currentUser);
        var handler = new DepositHandler(_accounts, guard, _transactions, _currentUser, _uow);

        var act = async () => await handler.HandleAsync(new DepositCommand(account.Id.Value, 100m));

        await act.Should().ThrowAsync<ForbiddenException>();
        await _transactions.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AdminBypassesBlockedGuard_CreatesTransaction()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateOwnedAccount(ownerId);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.UserId.Returns(ownerId);
        _currentUser.IsAdmin.Returns(true);

        var customers = Substitute.For<MiniBank.Domain.CustomerAggregate.ICustomerRepository>();
        customers.GetByIdAsync(Arg.Any<CustomerId>(), Arg.Any<CancellationToken>())
            .Returns((MiniBank.Domain.CustomerAggregate.Customer?)null); // guard must not even query

        var guard = new MiniBank.Features.CustomerAccessGuard(customers, _currentUser);
        var handler = new DepositHandler(_accounts, guard, _transactions, _currentUser, _uow);

        var response = await handler.HandleAsync(new DepositCommand(account.Id.Value, 100m));

        response.Amount.Should().Be(100m);
        await customers.DidNotReceive().GetByIdAsync(Arg.Any<CustomerId>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
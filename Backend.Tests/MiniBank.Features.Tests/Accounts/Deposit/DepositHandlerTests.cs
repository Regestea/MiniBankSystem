using FluentAssertions;
using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Features.Accounts.Deposit;
using NSubstitute;

namespace MiniBank.Features.Tests.Accounts.Deposit;

public sealed class DepositHandlerTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly ITransactionRepository _transactions = Substitute.For<ITransactionRepository>();
    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private DepositHandler CreateHandler() => new(_accounts, _transactions, _currentUser, _uow);

    private static Account CreateOwnedAccount(Guid customerId) =>
        Account.Open(new Domain.CustomerAggregate.ValueObjects.CustomerId(customerId), AccountType.Current);

    [Fact]
    public async Task HandleAsync_OwnedAccount_CreatesTransactionAndPersists()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateOwnedAccount(ownerId);
        _accounts.LoadAsync(new AccountId(account.Id.Value), Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(ownerId);

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
        _currentUser.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.NewGuid()); // different user

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new DepositCommand(account.Id.Value, 100m));

        await act.Should().ThrowAsync<ForbiddenException>();
        await _transactions.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UserWithoutCustomer_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateOwnedAccount(ownerId);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new DepositCommand(account.Id.Value, 100m));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task HandleAsync_FrozenAccount_ThrowsOperationNotAllowed()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateOwnedAccount(ownerId);
        account.Freeze();
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(ownerId);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new DepositCommand(account.Id.Value, 100m));

        await act.Should().ThrowAsync<DomainOperationNotAllowedException>();
        await _transactions.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }
}

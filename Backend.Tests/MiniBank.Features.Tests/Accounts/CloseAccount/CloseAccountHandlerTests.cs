using FluentAssertions;
using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Features.Accounts.CloseAccount;
using NSubstitute;

namespace MiniBank.Features.Tests.Accounts.CloseAccount;

public sealed class CloseAccountHandlerTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CloseAccountHandler CreateHandler() => new(_accounts, _currentUser, _uow);

    private static Account CreateAccount(Guid ownerId, decimal balance = 0m, AccountStatus status = AccountStatus.Active)
    {
        var acc = Account.Open(new Domain.CustomerAggregate.ValueObjects.CustomerId(ownerId), AccountType.Current);
        if (balance > 0)
            acc.Deposit(Money.FromDecimal(balance));
        if (status == AccountStatus.Frozen) acc.Freeze();
        return acc;
    }

    [Fact]
    public async Task HandleAsync_ZeroBalanceActive_Closes()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(ownerId, 0m);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(ownerId);

        var handler = CreateHandler();
        var response = await handler.HandleAsync(new CloseAccountCommand(account.Id.Value));

        response.Status.Should().Be("Closed");
        account.Status.Should().Be(AccountStatus.Closed);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AccountNotFound_ThrowsNotFound()
    {
        _accounts.LoadAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns((Account?)null);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new CloseAccountCommand(Guid.NewGuid()));
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_NotOwned_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(ownerId);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new CloseAccountCommand(account.Id.Value));
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task HandleAsync_NonZeroBalance_ThrowsInvariantViolation()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(ownerId, 100m);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(ownerId);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new CloseAccountCommand(account.Id.Value));
        await act.Should().ThrowAsync<DomainInvariantViolationException>()
            .Where(ex => ex.Details.ToString()!.Contains("non-zero"));
    }

    [Fact]
    public async Task HandleAsync_FrozenAccount_ThrowsNotAllowed()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(ownerId, 0m, AccountStatus.Frozen);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(ownerId);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new CloseAccountCommand(account.Id.Value));
        await act.Should().ThrowAsync<DomainOperationNotAllowedException>()
            .Where(ex => ex.Details.ToString()!.Contains("Frozen"));
    }

    [Fact]
    public async Task HandleAsync_AlreadyClosed_ThrowsNotAllowed()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(ownerId, 0m);
        account.Close();
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(ownerId);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new CloseAccountCommand(account.Id.Value));
        await act.Should().ThrowAsync<DomainOperationNotAllowedException>();
    }
}

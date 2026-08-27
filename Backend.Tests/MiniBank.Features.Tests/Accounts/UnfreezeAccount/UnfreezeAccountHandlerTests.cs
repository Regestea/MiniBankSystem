using FluentAssertions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Accounts.UnfreezeAccount;
using NSubstitute;

namespace MiniBank.Features.Tests.Accounts.UnfreezeAccount;

public sealed class UnfreezeAccountHandlerTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private UnfreezeAccountHandler CreateHandler() => new(_accounts, _uow);

    private static Account CreateFrozenAccount()
    {
        var acc = Account.Open(new Domain.CustomerAggregate.ValueObjects.CustomerId(Guid.NewGuid()), AccountType.Current);
        acc.Freeze();
        return acc;
    }

    [Fact]
    public async Task HandleAsync_FrozenAccount_Unfreezes()
    {
        var account = CreateFrozenAccount();
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        var handler = CreateHandler();

        var response = await handler.HandleAsync(new UnfreezeAccountCommand(account.Id.Value));

        response.Status.Should().Be("Active");
        account.Status.Should().Be(AccountStatus.Active);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsNotFound()
    {
        _accounts.LoadAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns((Account?)null);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new UnfreezeAccountCommand(Guid.NewGuid()));
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_ActiveAccount_ThrowsNotAllowed()
    {
        var account = Account.Open(new Domain.CustomerAggregate.ValueObjects.CustomerId(Guid.NewGuid()), AccountType.Current);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new UnfreezeAccountCommand(account.Id.Value));
        await act.Should().ThrowAsync<DomainOperationNotAllowedException>()
            .Where(ex => ex.Details.ToString()!.Contains("Only frozen"));
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ClosedAccount_ThrowsNotAllowed()
    {
        var account = Account.Open(new Domain.CustomerAggregate.ValueObjects.CustomerId(Guid.NewGuid()), AccountType.Current);
        account.Close();
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new UnfreezeAccountCommand(account.Id.Value));
        await act.Should().ThrowAsync<DomainOperationNotAllowedException>();
    }
}

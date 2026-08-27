using FluentAssertions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Accounts.FreezeAccount;
using NSubstitute;

namespace MiniBank.Features.Tests.Accounts.FreezeAccount;

public sealed class FreezeAccountHandlerTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private FreezeAccountHandler CreateHandler() => new(_accounts, _uow);

    private static Account CreateActiveAccount() =>
        Account.Open(new Domain.CustomerAggregate.ValueObjects.CustomerId(Guid.NewGuid()), AccountType.Current);

    [Fact]
    public async Task HandleAsync_ActiveAccount_Freezes()
    {
        var account = CreateActiveAccount();
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        var handler = CreateHandler();

        var response = await handler.HandleAsync(new FreezeAccountCommand(account.Id.Value));

        response.Status.Should().Be("Frozen");
        account.Status.Should().Be(AccountStatus.Frozen);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsNotFound()
    {
        _accounts.LoadAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns((Account?)null);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new FreezeAccountCommand(Guid.NewGuid()));
        await act.Should().ThrowAsync<NotFoundException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyFrozen_ThrowsNotAllowed()
    {
        var account = CreateActiveAccount();
        account.Freeze();
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new FreezeAccountCommand(account.Id.Value));
        await act.Should().ThrowAsync<DomainOperationNotAllowedException>()
            .Where(ex => ex.Details.ToString()!.Contains("already frozen"));
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ClosedAccount_ThrowsNotAllowed()
    {
        var account = CreateActiveAccount();
        // need zero balance to close
        account.Close();
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new FreezeAccountCommand(account.Id.Value));
        await act.Should().ThrowAsync<DomainOperationNotAllowedException>()
            .Where(ex => ex.Details.ToString()!.Contains("Closed"));
    }
}

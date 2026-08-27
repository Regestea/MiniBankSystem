using FluentAssertions;
using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Features.Accounts.Transfer;
using NSubstitute;

namespace MiniBank.Features.Tests.Accounts.Transfer;

public sealed class TransferHandlerTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly ITransactionRepository _transactions = Substitute.For<ITransactionRepository>();
    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private TransferHandler CreateHandler() => new(_accounts, _transactions, _currentUser, _uow);

    private static Account CreateAccount(Guid ownerId, decimal balance = 1000m)
    {
        var acc = Account.Open(new CustomerId(ownerId), AccountType.Current);
        acc.Deposit(MiniBank.Domain.BuildingBlocks.ValueObjects.Money.FromDecimal(balance));
        return acc;
    }

    [Fact]
    public async Task HandleAsync_OwnedSource_SufficientFunds_Transfers()
    {
        var ownerId = Guid.NewGuid();
        var from = CreateAccount(ownerId, 1000m);
        var to = Account.Open(new CustomerId(Guid.NewGuid()), AccountType.Current);
        _accounts.LoadAsync(from.Id, Arg.Any<CancellationToken>()).Returns(from);
        _accounts.LoadAsync(to.Id, Arg.Any<CancellationToken>()).Returns(to);
        _currentUser.UserId.Returns(ownerId);

        var handler = CreateHandler();
        var response = await handler.HandleAsync(new TransferCommand(from.Id.Value, to.Id.Value, 300m));

        response.Amount.Should().Be(300m);
        from.Balance.Amount.Should().Be(700m);
        to.Balance.Amount.Should().Be(300m);
        await _transactions.Received(1).AddAsync(Arg.Is<Transaction>(t => t.Type == TransactionType.Transfer), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SourceNotFound_ThrowsNotFound()
    {
        _accounts.LoadAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns((Account?)null);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new TransferCommand(Guid.NewGuid(), Guid.NewGuid(), 100m));
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_DestinationNotFound_ThrowsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var from = CreateAccount(ownerId);
        _accounts.LoadAsync(from.Id, Arg.Any<CancellationToken>()).Returns(from);
        _accounts.LoadAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns((Account?)null);
        _currentUser.UserId.Returns(ownerId);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new TransferCommand(from.Id.Value, Guid.NewGuid(), 100m));
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_NotOwned_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var from = CreateAccount(ownerId);
        var to = Account.Open(new CustomerId(Guid.NewGuid()), AccountType.Current);
        _accounts.LoadAsync(from.Id, Arg.Any<CancellationToken>()).Returns(from);
        _accounts.LoadAsync(to.Id, Arg.Any<CancellationToken>()).Returns(to);
        _currentUser.UserId.Returns(Guid.NewGuid());

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new TransferCommand(from.Id.Value, to.Id.Value, 100m));
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task HandleAsync_InsufficientFunds_ThrowsInvariantViolation()
    {
        var ownerId = Guid.NewGuid();
        var from = CreateAccount(ownerId, 100m);
        var to = Account.Open(new CustomerId(Guid.NewGuid()), AccountType.Current);
        _accounts.LoadAsync(from.Id, Arg.Any<CancellationToken>()).Returns(from);
        _accounts.LoadAsync(to.Id, Arg.Any<CancellationToken>()).Returns(to);
        _currentUser.UserId.Returns(ownerId);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new TransferCommand(from.Id.Value, to.Id.Value, 200m));
        await act.Should().ThrowAsync<DomainInvariantViolationException>();
    }
}
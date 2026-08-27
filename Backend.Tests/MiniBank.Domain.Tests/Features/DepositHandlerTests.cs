using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Features.Accounts.Deposit;

namespace MiniBank.Domain.Tests.Features;

public class DepositHandlerTests
{
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly CustomerId _ownerId = new(Guid.NewGuid());

    private DepositHandler BuildHandler(Guid? callerCustomerId)
        => new(_accounts, _transactions, new FakeCurrentUserContext(callerCustomerId), _uow);

    [Fact]
    public async Task Handle_OwnedAccount_CreatesTransactionAndPersists()
    {
        var account = Account.Open(_ownerId, AccountType.Current);
        await _accounts.AddAsync(account);
        var handler = BuildHandler(_ownerId.Value);

        var response = await handler.HandleAsync(new DepositCommand(account.Id.Value, 500m));

        Assert.Equal(500m, response.Amount);
        Assert.Single(account.Ledger);                 // posting appended to aggregate ledger
        Assert.Single(_transactions.Store);            // journal persisted
        Assert.Equal(1, _uow.SaveCount);               // single atomic commit
        Assert.Equal(500m, account.Balance.Amount);
    }

    [Fact]
    public async Task Handle_AccountNotOwned_ThrowsForbidden()
    {
        var account = Account.Open(_ownerId, AccountType.Savings);
        await _accounts.AddAsync(account);
        var otherCustomer = Guid.NewGuid();
        var handler = BuildHandler(otherCustomer);     // different customer

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.HandleAsync(new DepositCommand(account.Id.Value, 100m)));
        Assert.Empty(_transactions.Store);             // nothing persisted
    }

    [Fact]
    public async Task Handle_UserWithoutCustomer_ThrowsForbidden()
    {
        var account = Account.Open(_ownerId, AccountType.Current);
        await _accounts.AddAsync(account);
        var handler = BuildHandler(null);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.HandleAsync(new DepositCommand(account.Id.Value, 100m)));
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsNotFound()
    {
        var handler = BuildHandler(_ownerId.Value);
        var randomId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.HandleAsync(new DepositCommand(randomId, 100m)));

        Assert.Contains(randomId.ToString(), ex.Details.ToString());
    }

    [Fact]
    public async Task Handle_FrozenAccount_ThrowsOperationNotAllowed()
    {
        var account = Account.Open(_ownerId, AccountType.Current);
        await _accounts.AddAsync(account);
        account.Freeze();
        var handler = BuildHandler(_ownerId.Value);

        await Assert.ThrowsAsync<DomainOperationNotAllowedException>(
            () => handler.HandleAsync(new DepositCommand(account.Id.Value, 100m)));
    }
}

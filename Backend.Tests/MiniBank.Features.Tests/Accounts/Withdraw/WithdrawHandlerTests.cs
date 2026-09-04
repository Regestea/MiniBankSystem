using FluentAssertions;
using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Domain.RiskAggregate;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Features.Accounts.Withdraw;
using MiniBank.Features.Customers;
using NSubstitute;

namespace MiniBank.Features.Tests.Accounts.Withdraw;

public sealed class WithdrawHandlerTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly ITransactionRepository _transactions = Substitute.For<ITransactionRepository>();
    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRiskRepository _riskRepo = Substitute.For<IRiskRepository>();

    private readonly ICustomerAccessGuard _customerAccess = Substitute.For<ICustomerAccessGuard>();

    private void SetupRiskForCustomer(Guid customerId)
    {
        var risk = CustomerRisk.Create(customerId);
        _riskRepo.GetByCustomerIdAsync(customerId, Arg.Any<CancellationToken>()).Returns(risk);
    }

    private WithdrawHandler CreateHandler() => new(_accounts, _customerAccess, _transactions, _riskRepo, _currentUser, _uow);

    private static Account CreateFundedAccount(Guid ownerId, decimal initialDeposit = 1000m)
    {
        var acc = Account.Open(new CustomerId(ownerId), AccountType.Current);
        acc.Approve();
        acc.Deposit(MiniBank.Domain.BuildingBlocks.ValueObjects.Money.FromDecimal(initialDeposit));
        return acc;
    }

    [Fact]
    public async Task HandleAsync_OwnedAccount_SufficientFunds_Withdraws()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateFundedAccount(ownerId, 1000m);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.UserId.Returns(ownerId);
        SetupRiskForCustomer(ownerId);

        var handler = CreateHandler();
        var response = await handler.HandleAsync(new WithdrawCommand(account.Id.Value, 400m, "test-withdraw-1"));

        response.Amount.Should().Be(400m);
        account.Balance.Amount.Should().Be(600m);
        await _transactions.Received(1).AddAsync(Arg.Is<Transaction>(t => t.Amount.Amount == 400m), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InsufficientFunds_ThrowsInvariantViolation()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateFundedAccount(ownerId, 200m);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.UserId.Returns(ownerId);
        SetupRiskForCustomer(ownerId);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new WithdrawCommand(account.Id.Value, 500m, "test-withdraw-insufficient"));

        await act.Should().ThrowAsync<DomainInvariantViolationException>()
            .Where(ex => ex.Details.ToString()!.Contains("Insufficient funds"));
        await _transactions.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AccountNotFound_ThrowsNotFound()
    {
        _accounts.LoadAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns((Account?)null);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new WithdrawCommand(Guid.NewGuid(), 100m, "test-withdraw-notfound"));
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_NotOwned_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateFundedAccount(ownerId);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.UserId.Returns(Guid.NewGuid());

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new WithdrawCommand(account.Id.Value, 100m, "test-withdraw-notowned"));
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task HandleAsync_MissingRisk_ThrowsUnprocessable()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateFundedAccount(ownerId);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.UserId.Returns(ownerId);
        _riskRepo.GetByCustomerIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns((CustomerRisk?)null);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new WithdrawCommand(account.Id.Value, 100m, "no-risk-w"));

        await act.Should().ThrowAsync<DomainInvariantViolationException>()
            .Where(ex => ex.StatusCode == MiniBank.Domain.BuildingBlocks.Exceptions.ExceptionStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task HandleAsync_DailyLimitExceeded_ThrowsUnprocessable()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateFundedAccount(ownerId, 5000m);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.UserId.Returns(ownerId);
        var risk = CustomerRisk.Create(ownerId);
        risk.SetRiskLevel(MiniBank.Domain.RiskAggregate.RiskLevel.High);
        _riskRepo.GetByCustomerIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(risk);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new WithdrawCommand(account.Id.Value, 2000m, "over-limit-w"));

        await act.Should().ThrowAsync<DomainInvariantViolationException>()
            .Where(ex => ex.Details.ToString()!.Contains("Daily transaction limit exceeded"));
    }

    [Fact]
    public async Task HandleAsync_SameKeySamePayload_ReplaysOriginal()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateFundedAccount(ownerId);
        _accounts.LoadAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _currentUser.UserId.Returns(ownerId);
        SetupRiskForCustomer(ownerId);

        var existing = Transaction.CreateWithdraw(
            account.Id,
            MiniBank.Domain.BuildingBlocks.ValueObjects.Money.FromDecimal(100m),
            account.Balance,
            "existing",
            "replay-withdraw-1");
        _transactions.GetByReferenceIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = CreateHandler();
        var response = await handler.HandleAsync(new WithdrawCommand(account.Id.Value, 100m, "replay-withdraw-1"));

        response.TransactionId.Should().Be(existing.Id.Value);
        await _transactions.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }
}
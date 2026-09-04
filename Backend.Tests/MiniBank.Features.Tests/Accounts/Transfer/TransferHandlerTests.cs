using FluentAssertions;
using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Domain.RiskAggregate;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Features.Accounts.Transfer;
using MiniBank.Features.Customers;
using NSubstitute;

namespace MiniBank.Features.Tests.Accounts.Transfer;

public sealed class TransferHandlerTests
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

    private TransferHandler CreateHandler() => new(_accounts, _customerAccess, _transactions, _riskRepo, _currentUser, _uow);

    private static Account CreateAccount(Guid ownerId, decimal balance = 1000m)
    {
        var acc = Account.Open(new CustomerId(ownerId), AccountType.Current);
        acc.Approve();
        acc.Deposit(MiniBank.Domain.BuildingBlocks.ValueObjects.Money.FromDecimal(balance));
        return acc;
    }

    [Fact]
    public async Task HandleAsync_OwnedSource_SufficientFunds_Transfers()
    {
        var ownerId = Guid.NewGuid();
        var from = CreateAccount(ownerId, 1000m);
        var to = Account.Open(new CustomerId(Guid.NewGuid()), AccountType.Current);
        to.Approve();
        _accounts.LoadAsync(from.Id, Arg.Any<CancellationToken>()).Returns(from);
        _accounts.LoadAsync(to.Id, Arg.Any<CancellationToken>()).Returns(to);
        _currentUser.UserId.Returns(ownerId);
        SetupRiskForCustomer(ownerId);

        var handler = CreateHandler();
        var response = await handler.HandleAsync(new TransferCommand(from.Id.Value, to.Id.Value, 300m, "test-key-1"));

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

        var act = async () => await handler.HandleAsync(new TransferCommand(Guid.NewGuid(), Guid.NewGuid(), 100m, "test-key-src-not-found"));
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
        var act = async () => await handler.HandleAsync(new TransferCommand(from.Id.Value, Guid.NewGuid(), 100m, "test-key-dest-notfound"));
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
        var act = async () => await handler.HandleAsync(new TransferCommand(from.Id.Value, to.Id.Value, 100m, "test-key-not-owned"));
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task HandleAsync_InsufficientFunds_ThrowsInvariantViolation()
    {
        var ownerId = Guid.NewGuid();
        var from = CreateAccount(ownerId, 100m);
        var to = Account.Open(new CustomerId(Guid.NewGuid()), AccountType.Current);
        to.Approve();
        _accounts.LoadAsync(from.Id, Arg.Any<CancellationToken>()).Returns(from);
        _accounts.LoadAsync(to.Id, Arg.Any<CancellationToken>()).Returns(to);
        _currentUser.UserId.Returns(ownerId);
        SetupRiskForCustomer(ownerId);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new TransferCommand(from.Id.Value, to.Id.Value, 200m, "test-key-insufficient"));
        await act.Should().ThrowAsync<DomainInvariantViolationException>();
    }

    [Fact]
    public async Task HandleAsync_SameKeyDifferentAmount_ThrowsConflict()
    {
        var ownerId = Guid.NewGuid();
        var from = CreateAccount(ownerId, 1000m);
        var to = Account.Open(new CustomerId(Guid.NewGuid()), AccountType.Current);
        to.Approve();
        _accounts.LoadAsync(from.Id, Arg.Any<CancellationToken>()).Returns(from);
        _accounts.LoadAsync(to.Id, Arg.Any<CancellationToken>()).Returns(to);
        _currentUser.UserId.Returns(ownerId);
        SetupRiskForCustomer(ownerId);

        var existing = Transaction.CreateTransfer(
            from.Id,
            to.Id,
            MiniBank.Domain.BuildingBlocks.ValueObjects.Money.FromDecimal(100m),
            from.Balance,
            "existing",
            "dup-transfer-1");
        _transactions.GetByReferenceIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new TransferCommand(from.Id.Value, to.Id.Value, 999m, "dup-transfer-1"));

        await act.Should().ThrowAsync<DomainConflictException>();
        await _transactions.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhitespaceKey_ThrowsValidation()
    {
        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new TransferCommand(Guid.NewGuid(), Guid.NewGuid(), 100m, "   "));

        await act.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task HandleAsync_SameKeySamePayload_ReplaysOriginal()
    {
        var ownerId = Guid.NewGuid();
        var from = CreateAccount(ownerId, 1000m);
        var to = Account.Open(new CustomerId(Guid.NewGuid()), AccountType.Current);
        to.Approve();
        _accounts.LoadAsync(from.Id, Arg.Any<CancellationToken>()).Returns(from);
        _accounts.LoadAsync(to.Id, Arg.Any<CancellationToken>()).Returns(to);
        _currentUser.UserId.Returns(ownerId);
        SetupRiskForCustomer(ownerId);

        var existing = Transaction.CreateTransfer(
            from.Id,
            to.Id,
            MiniBank.Domain.BuildingBlocks.ValueObjects.Money.FromDecimal(100m),
            from.Balance,
            "existing",
            "replay-transfer-1");
        _transactions.GetByReferenceIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = CreateHandler();
        var response = await handler.HandleAsync(new TransferCommand(from.Id.Value, to.Id.Value, 100m, "replay-transfer-1"));

        response.TransactionId.Should().Be(existing.Id.Value);
        await _transactions.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MissingRisk_ThrowsUnprocessable()
    {
        var ownerId = Guid.NewGuid();
        var from = CreateAccount(ownerId, 1000m);
        var to = Account.Open(new CustomerId(Guid.NewGuid()), AccountType.Current);
        to.Approve();
        _accounts.LoadAsync(from.Id, Arg.Any<CancellationToken>()).Returns(from);
        _accounts.LoadAsync(to.Id, Arg.Any<CancellationToken>()).Returns(to);
        _currentUser.UserId.Returns(ownerId);
        _riskRepo.GetByCustomerIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns((CustomerRisk?)null);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new TransferCommand(from.Id.Value, to.Id.Value, 100m, "no-risk-1"));

        await act.Should().ThrowAsync<DomainInvariantViolationException>()
            .Where(ex => ex.StatusCode == MiniBank.Domain.BuildingBlocks.Exceptions.ExceptionStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task HandleAsync_DailyLimitExceeded_ThrowsUnprocessable()
    {
        var ownerId = Guid.NewGuid();
        var from = CreateAccount(ownerId, 5000m);
        var to = Account.Open(new CustomerId(Guid.NewGuid()), AccountType.Current);
        to.Approve();
        _accounts.LoadAsync(from.Id, Arg.Any<CancellationToken>()).Returns(from);
        _accounts.LoadAsync(to.Id, Arg.Any<CancellationToken>()).Returns(to);
        _currentUser.UserId.Returns(ownerId);
        var risk = CustomerRisk.Create(ownerId);
        risk.SetRiskLevel(MiniBank.Domain.RiskAggregate.RiskLevel.High);
        _riskRepo.GetByCustomerIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(risk);

        var handler = CreateHandler();
        var act = async () => await handler.HandleAsync(new TransferCommand(from.Id.Value, to.Id.Value, 2000m, "over-limit-1"));

        await act.Should().ThrowAsync<DomainInvariantViolationException>()
            .Where(ex => ex.Details.ToString()!.Contains("Daily transaction limit exceeded"));
    }
}
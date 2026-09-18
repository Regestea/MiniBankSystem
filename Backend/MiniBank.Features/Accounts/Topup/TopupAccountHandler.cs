using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.Topup;

/// <summary>Fake gateway charge — always approves valid amounts, deposits into the caller's account.</summary>
internal sealed class TopupAccountHandler(
    IAccountRepository accounts,
    ICustomerAccessGuard customerAccess,
    ITransactionRepository transactions,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<TopupAccountCommand, TransactionResponse>
{
    public async Task<TransactionResponse> HandleAsync(TopupAccountCommand command, CancellationToken cancellationToken = default)
    {
        // Simulated gateway: only the amount matters. Each call is a new charge,
        // so we mint a fresh gateway reference as the idempotency key.
        var gatewayReference = $"topup-{Guid.NewGuid():N}";

        var account = await accounts.LoadAsync(command.AccountId, cancellationToken)
            ?? throw new NotFoundException("account", command.AccountId);

        AccountOwnership.EnsureOwnedByCaller(account.CustomerId, currentUser);
        await customerAccess.EnsureNotBlockedAsync(account.CustomerId, cancellationToken);

        var (tx, _) = account.Deposit(
            Money.FromDecimal(command.Amount),
            description: "Fake-gateway top-up",
            referenceId: gatewayReference);

        await transactions.AddAsync(tx, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new TransactionResponse(tx.Id.Value, tx.Type.ToString(), tx.Amount.Amount,
                                       tx.ReferenceId, tx.OccurredOn);
    }
}

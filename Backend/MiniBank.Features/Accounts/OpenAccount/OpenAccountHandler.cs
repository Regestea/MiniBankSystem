using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.OpenAccount;

internal sealed class OpenAccountHandler(
    IAccountRepository accounts,
    ICustomerRepository customers,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<OpenAccountCommand, AccountResponse>
{
    public async Task<AccountResponse> HandleAsync(OpenAccountCommand command, CancellationToken cancellationToken = default)
    {
        var callerCustomerId = new CustomerId(currentUser.UserId);

        var customer = await customers.GetByIdAsync(callerCustomerId, cancellationToken)
            ?? throw new NotFoundException("customer", callerCustomerId);

        if (customer.Status != CustomerStatus.Verified)
            throw new DomainOperationNotAllowedException(nameof(customer.Status),
                $"Only verified customers can open accounts. Current status: {customer.Status}.");

        var accountType = command.AccountType switch
        {
            "Savings" => AccountType.Savings,
            "Current" => AccountType.Current,
            // Validator rejects anything else, but handlers must never silently fall back
            // when called directly (mediator bypass in tests/other callers).
            _ => throw new DomainValidationException(nameof(command.AccountType), "AccountType must be 'Current' or 'Savings'.")
        };

        var account = Account.Open(customer.Id, accountType);

        await accounts.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AccountResponse.From(account);
    }
}

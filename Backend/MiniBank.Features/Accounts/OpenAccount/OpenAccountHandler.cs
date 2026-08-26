using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Features.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.OpenAccount;

internal sealed class OpenAccountHandler(
    IAccountRepository accounts,
    ICustomerRepository customers,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<OpenAccountCommand, AccountResponse>
{
    public async Task<AccountResponse> Handle(OpenAccountCommand command, CancellationToken cancellationToken = default)
    {
        // Banking rule: only Verified customers may open accounts
        var callerCustomerId = await currentUser.GetCustomerIdAsync(cancellationToken)
            ?? throw new ForbiddenException("customer", "User has no linked customer profile.");

        var customer = await customers.GetByIdAsync(callerCustomerId, cancellationToken)
            ?? throw new NotFoundException("customer", callerCustomerId);

        if (customer.Status != CustomerStatus.Verified)
            throw new DomainOperationNotAllowedException(nameof(customer.Status),
                $"Only verified customers can open accounts. Current status: {customer.Status}.");

        var accountType = command.AccountType == "Savings"
            ? AccountType.Savings
            : AccountType.Current;

        var account = Account.Open(customer.Id, accountType); // raises AccountOpenedEvent, status Active

        await accounts.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AccountResponse.From(account);
    }
}

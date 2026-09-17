using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Features.Customers;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.UpdateCurrentCustomer;

/// <summary>Updates the caller's own profile — the target id is the token subject, never client input.</summary>
internal sealed class UpdateCurrentCustomerHandler(
    ICustomerRepository customers,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateCurrentCustomerCommand, CustomerResponse>
{
    public async Task<CustomerResponse> HandleAsync(UpdateCurrentCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customerId = new CustomerId(currentUser.UserId);

        var customer = await customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException("customer", customerId);

        customer.UpdateInformation(new FullName(command.FullName), customer.Email, new PhoneNumber(command.PhoneNumber));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerResponse.From(customer);
    }
}

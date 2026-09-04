using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Features.Customers;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.UpdateCustomer;

/// <summary>Updates a customer's profile. Owners update their own; admins may update any (consistent with GetCustomer).</summary>
internal sealed class UpdateCustomerHandler(
    ICustomerRepository customers,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateCustomerCommand, CustomerResponse>
{
    public async Task<CustomerResponse> HandleAsync(UpdateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAdmin && command.CustomerId != currentUser.UserId)
            throw new ForbiddenException("customer", "Profile can only be updated by its owner.");

        var customerId = new CustomerId(command.CustomerId);

        var customer = await customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException("customer", customerId);

        customer.UpdateInformation(new FullName(command.FullName), customer.Email, new PhoneNumber(command.PhoneNumber));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerResponse.From(customer);
    }
}

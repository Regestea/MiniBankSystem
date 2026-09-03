using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Features.Customers;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.UpdateCustomer;

/// <summary>Updates a customer's profile. Only the owner can update it (self-service).</summary>
internal sealed class UpdateCustomerHandler(
    ICustomerRepository customers,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateCustomerCommand, CustomerResponse>
{
    public async Task<CustomerResponse> HandleAsync(UpdateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        if (command.CustomerId != currentUser.UserId)
            throw new ForbiddenException("customer", "Profile can only be updated by its owner.");

        var customerId = new CustomerId(currentUser.UserId);

        var customer = await customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException("customer", customerId);

        customer.UpdateInformation(new FullName(command.FullName), customer.Email, new PhoneNumber(command.PhoneNumber));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerResponse.From(customer);
    }
}

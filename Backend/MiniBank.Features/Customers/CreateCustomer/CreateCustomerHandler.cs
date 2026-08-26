using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.CreateCustomer;

internal sealed class CreateCustomerHandler(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateCustomerCommand, CustomerResponse>
{
    public async Task<CustomerResponse> Handle(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        if (await customers.EmailExistsAsync(command.Email, cancellationToken))
            throw new DomainConflictException(nameof(command.Email), "Email already registered.");

        // Domain factory validates VOs and starts lifecycle at Pending
        var customer = Customer.Create(command.FullName, command.Email, command.PhoneNumber);

        await customers.AddAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerResponse.From(customer);
    }
}

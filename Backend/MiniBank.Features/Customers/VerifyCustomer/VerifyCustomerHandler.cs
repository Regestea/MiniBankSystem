using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.VerifyCustomer;

internal sealed class VerifyCustomerHandler(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork) : ICommandHandler<VerifyCustomerCommand, VerifyResponse>
{
    public async Task<VerifyResponse> HandleAsync(VerifyCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(command.CustomerId, cancellationToken)
            ?? throw new NotFoundException("customer", command.CustomerId);

        customer.Verify();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new VerifyResponse(customer.Id.Value, customer.Status.ToString(), customer.Version);
    }
}

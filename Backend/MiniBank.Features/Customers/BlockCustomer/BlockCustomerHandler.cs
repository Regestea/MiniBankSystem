using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.BlockCustomer;

internal sealed class BlockCustomerHandler(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork) : ICommandHandler<BlockCustomerCommand, BlockResponse>
{
    public async Task<BlockResponse> Handle(BlockCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(command.CustomerId, cancellationToken)
            ?? throw new NotFoundException("customer", command.CustomerId);

        customer.Block();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BlockResponse(customer.Id.Value, customer.Status.ToString(), customer.Version);
    }
}

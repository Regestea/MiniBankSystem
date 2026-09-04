using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.BlockCustomer;

internal sealed class BlockCustomerHandler(
    ICustomerRepository customers,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<BlockCustomerCommand, BlockResponse>
{
    public async Task<BlockResponse> HandleAsync(BlockCustomerCommand command, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAdmin)
            throw new ForbiddenException("customer", "Only admins can block customers.");

        var customer = await customers.GetByIdAsync(command.CustomerId, cancellationToken)
            ?? throw new NotFoundException("customer", command.CustomerId);

        customer.Block();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BlockResponse(customer.Id.Value, customer.Status.ToString(), customer.Version);
    }
}

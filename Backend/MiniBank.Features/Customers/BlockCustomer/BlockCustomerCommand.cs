using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.BlockCustomer;

public sealed record BlockCustomerCommand(Guid CustomerId) : ICommand<BlockResponse>;

public sealed record BlockResponse(Guid CustomerId, string Status, int Version);

public sealed class BlockCustomerValidator : AbstractValidator<BlockCustomerCommand>
{
    public BlockCustomerValidator()
        => RuleFor(x => x.CustomerId).NotEmpty();
}

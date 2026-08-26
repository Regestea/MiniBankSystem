using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.VerifyCustomer;

public sealed record VerifyCustomerCommand(Guid CustomerId) : ICommand<VerifyResponse>;

public sealed record VerifyResponse(Guid CustomerId, string Status, int Version);

public sealed class VerifyCustomerValidator : AbstractValidator<VerifyCustomerCommand>
{
    public VerifyCustomerValidator()
        => RuleFor(x => x.CustomerId).NotEmpty();
}

using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.UpdateCustomer;

public sealed record UpdateCustomerCommand(
    Guid CustomerId,
    string FullName,
    string PhoneNumber) : ICommand<CustomerResponse>;

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\d{10,15}$")
            .WithMessage("Phone number must be 10-15 digits.");
    }
}

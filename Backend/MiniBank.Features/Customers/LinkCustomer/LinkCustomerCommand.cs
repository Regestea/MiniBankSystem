using FluentValidation;
using MiniBank.Features.Customers;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.LinkCustomer;

public sealed record LinkCustomerCommand(
    Guid UserId,
    string FullName,
    string PhoneNumber) : ICommand<CustomerResponse>;

public sealed class LinkCustomerValidator : AbstractValidator<LinkCustomerCommand>
{
    public LinkCustomerValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\d{10,15}$")
            .WithMessage("Phone number must be 10-15 digits.");
    }
}

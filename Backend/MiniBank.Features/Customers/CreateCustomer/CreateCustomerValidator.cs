using FluentValidation;

namespace MiniBank.Features.Customers.CreateCustomer;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\d{10,15}$")
            .WithMessage("Phone number must be 10-15 digits.");
    }
}

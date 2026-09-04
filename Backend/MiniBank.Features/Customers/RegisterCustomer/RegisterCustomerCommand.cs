using FluentValidation;
using MiniBank.Features.Customers;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.RegisterCustomer;

public sealed record RegisterCustomerCommand(
    string Email,
    string Password,
    string FullName,
    string PhoneNumber) : ICommand<CustomerResponse>;

public sealed class RegisterCustomerValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[!@#$%^&*(),.?""':{}|<>\-_=+\[\]\\\/`~]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\d{10,15}$")
            .WithMessage("Phone number must be 10-15 digits.");
    }
}

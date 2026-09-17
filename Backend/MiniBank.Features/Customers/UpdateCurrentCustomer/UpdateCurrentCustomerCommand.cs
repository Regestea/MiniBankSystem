using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.UpdateCurrentCustomer;

/// <summary>
/// Updates the authenticated caller's own profile.
/// Identity comes from the token; no CustomerId in the contract (no IDOR surface).
/// </summary>
public sealed record UpdateCurrentCustomerCommand(
    string FullName,
    string PhoneNumber) : ICommand<CustomerResponse>;

public sealed class UpdateCurrentCustomerValidator : AbstractValidator<UpdateCurrentCustomerCommand>
{
    public UpdateCurrentCustomerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\d{10,15}$")
            .WithMessage("Phone number must be 10-15 digits.");
    }
}

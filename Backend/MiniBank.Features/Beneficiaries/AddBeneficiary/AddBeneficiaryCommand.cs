using FluentValidation;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Beneficiaries.AddBeneficiary;

public sealed record AddBeneficiaryCommand(string AccountNumber, string? Nickname = null) : ICommand<Beneficiaries.ListBeneficiaries.BeneficiaryDto>;

public sealed class AddBeneficiaryValidator : AbstractValidator<AddBeneficiaryCommand>
{
    public AddBeneficiaryValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty()
            .Must(AccountNumber.IsValid)
            .WithMessage("Account number must be IR-XXXXXXXXXX (10 digits) or 16 digits, first digit non-zero.");
        RuleFor(x => x.Nickname)
            .MaximumLength(100)
            .When(x => x.Nickname is not null);
    }
}

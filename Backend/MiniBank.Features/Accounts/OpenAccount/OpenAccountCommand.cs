using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.OpenAccount;

public sealed record OpenAccountCommand(
    Guid RequesterUserId,
    string AccountType) : ICommand<AccountResponse>;

public sealed class OpenAccountValidator : AbstractValidator<OpenAccountCommand>
{
    public OpenAccountValidator()
        => RuleFor(x => x.AccountType).NotEmpty()
              .Must(t => t is "Current" or "Savings")
              .WithMessage("AccountType must be 'Current' or 'Savings'.");
}

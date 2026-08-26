using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.UnfreezeAccount;

public sealed record UnfreezeAccountCommand(Guid AccountId) : ICommand<AccountStatusResponse>;

public sealed class UnfreezeAccountValidator : AbstractValidator<UnfreezeAccountCommand>
{
    public UnfreezeAccountValidator()
        => RuleFor(x => x.AccountId).NotEmpty();
}

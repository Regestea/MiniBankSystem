using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.FreezeAccount;

public sealed record FreezeAccountCommand(Guid AccountId) : ICommand<AccountStatusResponse>;

public sealed class FreezeAccountValidator : AbstractValidator<FreezeAccountCommand>
{
    public FreezeAccountValidator()
        => RuleFor(x => x.AccountId).NotEmpty();
}

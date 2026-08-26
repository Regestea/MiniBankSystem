using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.CloseAccount;

public sealed record CloseAccountCommand(Guid AccountId) : ICommand<CloseAccountResponse>;

public sealed record CloseAccountResponse(Guid AccountId, string Status, int Version);

public sealed class CloseAccountValidator : AbstractValidator<CloseAccountCommand>
{
    public CloseAccountValidator()
        => RuleFor(x => x.AccountId).NotEmpty();
}

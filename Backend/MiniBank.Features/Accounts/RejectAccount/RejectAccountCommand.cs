using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.RejectAccount;

public sealed record RejectAccountCommand(
    Guid AccountId,
    string Reason
) : ICommand<RejectAccountResponse>;

public sealed record RejectAccountResponse(
    Guid AccountId,
    string Status,
    int Version);

public sealed class RejectAccountValidator : AbstractValidator<RejectAccountCommand>
{
    public RejectAccountValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

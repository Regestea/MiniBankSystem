using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.ApproveAccount;

public sealed record ApproveAccountCommand(Guid AccountId) : ICommand<ApproveAccountResponse>;

public sealed record ApproveAccountResponse(
    Guid AccountId,
    string Status,
    int Version);

public sealed class ApproveAccountValidator : AbstractValidator<ApproveAccountCommand>
{
    public ApproveAccountValidator()
        => RuleFor(x => x.AccountId).NotEmpty();
}

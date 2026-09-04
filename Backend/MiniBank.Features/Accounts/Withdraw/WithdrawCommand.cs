using FluentValidation;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.Withdraw;

public sealed record WithdrawCommand(
    Guid AccountId,
    decimal Amount,
    string IdempotencyKey) : ICommand<TransactionResponse>;

public sealed class WithdrawValidator : AbstractValidator<WithdrawCommand>
{
    public WithdrawValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0)
            .LessThanOrEqualTo(BankingRules.MaxTransactionAmount)
            .Must(a => decimal.Round(a, 2) == a)
            .WithMessage("Amount cannot have more than 2 decimal places.");
        RuleFor(x => x.IdempotencyKey).NotEmpty().Must(k => !string.IsNullOrWhiteSpace(k)).WithMessage("IdempotencyKey must not be empty or whitespace.").MaximumLength(BankingRules.MaxIdempotencyKeyLength);
    }
}

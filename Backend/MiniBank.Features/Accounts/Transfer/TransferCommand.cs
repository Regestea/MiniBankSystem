using FluentValidation;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.Transfer;

public sealed record TransferCommand(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string IdempotencyKey) : ICommand<TransferResponse>;

public sealed class TransferValidator : AbstractValidator<TransferCommand>
{
    public TransferValidator()
    {
        RuleFor(x => x.FromAccountId).NotEmpty();
        RuleFor(x => x.ToAccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0)
            .LessThanOrEqualTo(BankingRules.MaxTransactionAmount)
            .Must(a => decimal.Round(a, 2) == a)
            .WithMessage("Amount cannot have more than 2 decimal places.");
        RuleFor(x => x.IdempotencyKey).NotEmpty().Must(k => !string.IsNullOrWhiteSpace(k)).WithMessage("IdempotencyKey must not be empty or whitespace.").MaximumLength(BankingRules.MaxIdempotencyKeyLength);
        RuleFor(x => x)
            .Must(x => x.FromAccountId != x.ToAccountId)
            .WithMessage("Source and destination accounts must differ.")
            .WithName("Transfer");
    }
}

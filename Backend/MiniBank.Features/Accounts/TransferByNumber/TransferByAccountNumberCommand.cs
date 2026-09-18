using FluentValidation;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Features.Accounts;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.TransferByNumber;

/// <summary>
/// Step 3 of the account-number flow: transfer to a destination resolved by
/// account number (IR-XXXXXXXXXX or 16-digit). Source is one of the caller's own
/// accounts (Guid). Amounts are USD. Set SaveBeneficiary to remember the
/// destination for next transfers (the "remember" checkbox).
/// IdempotencyKey is optional — when omitted the server generates one, so the
/// UI confirm button only sends from/to/amount.
/// </summary>
public sealed record TransferByAccountNumberCommand(
    Guid FromAccountId,
    string ToAccountNumber,
    decimal Amount,
    string? IdempotencyKey = null,
    bool SaveBeneficiary = false) : ICommand<TransferResponse>;

public sealed class TransferByAccountNumberValidator : AbstractValidator<TransferByAccountNumberCommand>
{
    public TransferByAccountNumberValidator()
    {
        RuleFor(x => x.FromAccountId).NotEmpty();
        RuleFor(x => x.ToAccountNumber)
            .NotEmpty()
            .Must(AccountNumber.IsValid)
            .WithMessage("Account number must be IR-XXXXXXXXXX (10 digits) or 16 digits, first digit non-zero.");
        RuleFor(x => x.Amount).GreaterThan(0)
            .LessThanOrEqualTo(BankingRules.MaxTransactionAmount)
            .Must(a => decimal.Round(a, 2) == a)
            .WithMessage("Amount cannot have more than 2 decimal places.");
        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(BankingRules.MaxIdempotencyKeyLength)
            .When(x => x.IdempotencyKey is not null);
    }
}

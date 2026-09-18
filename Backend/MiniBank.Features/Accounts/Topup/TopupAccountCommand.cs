using FluentValidation;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Features.Accounts;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.Topup;

/// <summary>
/// Fake-gateway top-up: charges the caller's account with money from a simulated
/// payment gateway. The gateway only asks for the AMOUNT (USD) — no card, no
/// idempotency key from the client. The server generates the idempotency key
/// (topup-...) so double-clicks with the same generated key cannot double-charge;
/// each new request creates a new deposit (like a real gateway charge).
/// </summary>
public sealed record TopupAccountCommand(Guid AccountId, decimal Amount) : ICommand<TransactionResponse>;

public sealed class TopupAccountValidator : AbstractValidator<TopupAccountCommand>
{
    public TopupAccountValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0)
            .LessThanOrEqualTo(BankingRules.MaxTransactionAmount)
            .Must(a => decimal.Round(a, 2) == a)
            .WithMessage("Amount cannot have more than 2 decimal places.");
    }
}

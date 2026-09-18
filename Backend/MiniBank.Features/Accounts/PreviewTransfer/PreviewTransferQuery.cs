using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.PreviewTransfer;

/// <summary>
/// Previews a transfer destination — returns the masked holder name so the sender can
/// verify the receiver before moving money, without exposing full PII or balances.
/// Step 2 of the account-number flow returns the FULL holder name as well, so the
/// sender can confirm ("I am sending $X to Full Name") before the final confirm.
/// </summary>
public sealed record PreviewTransferQuery(Guid ToAccountId) : IQuery<PreviewTransferResponse>;

/// <summary>Preview by account number (IR-XXXXXXXXXX or 16-digit) — same response, resolved via account_number.</summary>
public sealed record PreviewTransferByNumberQuery(string AccountNumber) : IQuery<PreviewTransferResponse>;

public sealed record PreviewTransferResponse(
    Guid ToAccountId,
    string AccountNumber,
    string HolderFullName,
    string MaskedHolderName);

public sealed class PreviewTransferValidator : AbstractValidator<PreviewTransferQuery>
{
    public PreviewTransferValidator()
    {
        RuleFor(x => x.ToAccountId).NotEmpty();
    }
}

public sealed class PreviewTransferByNumberValidator : AbstractValidator<PreviewTransferByNumberQuery>
{
    public PreviewTransferByNumberValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty()
            .Must(n => Domain.AccountAggregate.ValueObjects.AccountNumber.IsValid(n))
            .WithMessage("Account number must be IR-XXXXXXXXXX (10 digits) or 16 digits, first digit non-zero.");
    }
}

using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.PreviewTransfer;

/// <summary>
/// Previews a transfer destination — returns the masked holder name so the sender can
/// verify the receiver before moving money, without exposing full PII or balances.
/// </summary>
public sealed record PreviewTransferQuery(Guid ToAccountId) : IQuery<PreviewTransferResponse>;

public sealed record PreviewTransferResponse(
    Guid ToAccountId,
    string AccountNumber,
    string MaskedHolderName);

public sealed class PreviewTransferValidator : AbstractValidator<PreviewTransferQuery>
{
    public PreviewTransferValidator()
    {
        RuleFor(x => x.ToAccountId).NotEmpty();
    }
}

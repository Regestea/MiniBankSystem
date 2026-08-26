using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Accounts.Transfer;

public sealed record TransferCommand(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount) : ICommand<TransferResponse>;

public sealed class TransferValidator : AbstractValidator<TransferCommand>
{
    public TransferValidator()
    {
        RuleFor(x => x.FromAccountId).NotEmpty();
        RuleFor(x => x.ToAccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0)
            .Must(a => decimal.Round(a, 2) == a)
            .WithMessage("Amount cannot have more than 2 decimal places.");
        RuleFor(x => x)
            .Must(x => x.FromAccountId != x.ToAccountId)
            .WithMessage("Source and destination accounts must differ.")
            .WithName("Transfer");
    }
}

using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Risk.UpdateRiskLevel;

public sealed record UpdateRiskLevelCommand(
    Guid CustomerId,
    string RiskLevel
) : ICommand<UpdateRiskLevelResponse>;

public sealed record UpdateRiskLevelResponse(
    Guid RiskId,
    string RiskLevel,
    decimal DailyTransactionLimit,
    int DailyTransactionCountLimit,
    int Version);

public sealed class UpdateRiskLevelValidator : AbstractValidator<UpdateRiskLevelCommand>
{
    public UpdateRiskLevelValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.RiskLevel).Must(x => x is "Low" or "Medium" or "High")
            .WithMessage("RiskLevel must be Low, Medium, or High.");
    }
}

using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Risk.GetCustomerRisk;

public sealed record GetCustomerRiskQuery(Guid CustomerId) : IQuery<GetCustomerRiskResponse>;

public sealed class GetCustomerRiskQueryValidator : AbstractValidator<GetCustomerRiskQuery>
{
    public GetCustomerRiskQueryValidator()
        => RuleFor(x => x.CustomerId).NotEmpty();
}

public sealed record GetCustomerRiskResponse(
    Guid RiskId,
    Guid CustomerId,
    string RiskLevel,
    decimal DailyTransactionLimit,
    int DailyTransactionCountLimit,
    int TransactionsToday,
    decimal AmountToday);

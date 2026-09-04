using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Domain.RiskAggregate;
using MiniBank.Domain.RiskAggregate.ValueObjects;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Risk.UpdateRiskLevel;

internal sealed class UpdateRiskLevelHandler(
    IRiskRepository riskRepo,
    ICustomerRepository customers,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateRiskLevelCommand, UpdateRiskLevelResponse>
{
    public async Task<UpdateRiskLevelResponse> HandleAsync(UpdateRiskLevelCommand command, CancellationToken cancellationToken = default)
    {
        // Guard against orphan risk rows for non-existent customers.
        var customer = await customers.GetByIdAsync(new CustomerId(command.CustomerId), cancellationToken)
            ?? throw new NotFoundException("customer", command.CustomerId);

        var risk = await riskRepo.GetByCustomerIdAsync(command.CustomerId, cancellationToken);

        if (risk is null)
        {
            risk = CustomerRisk.Create(customer.Id.Value);
            await riskRepo.AddAsync(risk, cancellationToken);
        }

        var level = command.RiskLevel switch
        {
            "Low" => RiskLevel.Low,
            "Medium" => RiskLevel.Medium,
            "High" => RiskLevel.High,
            _ => throw new DomainValidationException(nameof(command.RiskLevel), "Invalid risk level.")
        };

        risk.SetRiskLevel(level, currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateRiskLevelResponse(
            risk.Id.Value, risk.RiskLevel.ToString(),
            risk.DailyTransactionLimit, risk.DailyTransactionCountLimit,
            risk.Version);
    }
}

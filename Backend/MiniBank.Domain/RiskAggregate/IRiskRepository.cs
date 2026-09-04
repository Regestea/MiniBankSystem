using MiniBank.Domain.RiskAggregate.ValueObjects;

namespace MiniBank.Domain.RiskAggregate;

public interface IRiskRepository
{
    Task<CustomerRisk?> GetByIdAsync(CustomerRiskId id, CancellationToken cancellationToken = default);
    Task<CustomerRisk?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerRisk risk, CancellationToken cancellationToken = default);
}

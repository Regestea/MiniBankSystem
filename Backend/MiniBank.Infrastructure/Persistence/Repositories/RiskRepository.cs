using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.RiskAggregate;
using MiniBank.Domain.RiskAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Repositories;

internal sealed class RiskRepository(MiniBankDbContext db) : IRiskRepository
{
    public Task<CustomerRisk?> GetByIdAsync(CustomerRiskId id, CancellationToken cancellationToken = default)
        => db.CustomerRisks.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<CustomerRisk?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        => db.CustomerRisks.FirstOrDefaultAsync(r => r.CustomerId == customerId, cancellationToken);

    public async Task AddAsync(CustomerRisk risk, CancellationToken cancellationToken = default)
        => await db.CustomerRisks.AddAsync(risk, cancellationToken);
}

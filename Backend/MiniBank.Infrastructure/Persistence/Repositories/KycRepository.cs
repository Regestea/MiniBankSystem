using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.KycAggregate;
using MiniBank.Domain.KycAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Repositories;

internal sealed class KycRepository(MiniBankDbContext db) : IKycRepository
{
    public Task<KycVerification?> GetByIdAsync(KycVerificationId id, CancellationToken cancellationToken = default)
        => db.KycVerifications.FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

    public Task<KycVerification?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        => db.KycVerifications.FirstOrDefaultAsync(k => k.CustomerId == customerId, cancellationToken);

    public async Task<IReadOnlyList<KycVerification>> GetByStatusAsync(KycStatus status, CancellationToken cancellationToken = default)
        => await db.KycVerifications
            .Where(k => k.Status == status)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(KycVerification kyc, CancellationToken cancellationToken = default)
        => await db.KycVerifications.AddAsync(kyc, cancellationToken);
}

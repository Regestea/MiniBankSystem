using MiniBank.Domain.KycAggregate.ValueObjects;

namespace MiniBank.Domain.KycAggregate;

public interface IKycRepository
{
    Task<KycVerification?> GetByIdAsync(KycVerificationId id, CancellationToken cancellationToken = default);
    Task<KycVerification?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KycVerification>> GetByStatusAsync(KycStatus status, CancellationToken cancellationToken = default);
    Task AddAsync(KycVerification kyc, CancellationToken cancellationToken = default);
}

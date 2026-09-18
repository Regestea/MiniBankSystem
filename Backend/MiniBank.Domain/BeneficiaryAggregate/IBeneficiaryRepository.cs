using MiniBank.Domain.BeneficiaryAggregate.ValueObjects;

namespace MiniBank.Domain.BeneficiaryAggregate;

public interface IBeneficiaryRepository
{
    Task<Beneficiary?> GetByIdAsync(BeneficiaryId id, CancellationToken cancellationToken = default);

    Task<Beneficiary?> GetByOwnerAndNumberAsync(Guid ownerCustomerId, string accountNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Beneficiary>> ListByOwnerAsync(Guid ownerCustomerId, CancellationToken cancellationToken = default);

    Task AddAsync(Beneficiary beneficiary, CancellationToken cancellationToken = default);

    Task RemoveAsync(Beneficiary beneficiary, CancellationToken cancellationToken = default);
}

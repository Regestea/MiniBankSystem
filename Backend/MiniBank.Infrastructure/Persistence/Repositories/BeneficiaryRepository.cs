using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BeneficiaryAggregate;
using MiniBank.Domain.BeneficiaryAggregate.ValueObjects;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Repositories;

internal sealed class BeneficiaryRepository(MiniBankDbContext db) : IBeneficiaryRepository
{
    public Task<Beneficiary?> GetByIdAsync(BeneficiaryId id, CancellationToken cancellationToken = default)
        => db.Beneficiaries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Beneficiary?> GetByOwnerAndNumberAsync(Guid ownerCustomerId, string accountNumber, CancellationToken cancellationToken = default)
    {
        var normalized = AccountNumber.Normalize(accountNumber);
        if (!AccountNumber.IsValid(normalized))
            return Task.FromResult<Beneficiary?>(null);

        var owner = new CustomerId(ownerCustomerId);
        var number = new AccountNumber(normalized);
        return db.Beneficiaries.FirstOrDefaultAsync(
            x => x.OwnerCustomerId == owner && x.AccountNumber == number, cancellationToken);
    }

    public async Task<IReadOnlyList<Beneficiary>> ListByOwnerAsync(Guid ownerCustomerId, CancellationToken cancellationToken = default)
    {
        var owner = new CustomerId(ownerCustomerId);
        var list = await db.Beneficiaries
            .Where(x => x.OwnerCustomerId == owner)
            .OrderBy(x => x.HolderName)
            .ToListAsync(cancellationToken);
        return list;
    }

    public async Task AddAsync(Beneficiary beneficiary, CancellationToken cancellationToken = default)
        => await db.Beneficiaries.AddAsync(beneficiary, cancellationToken);

    public Task RemoveAsync(Beneficiary beneficiary, CancellationToken cancellationToken = default)
    {
        db.Beneficiaries.Remove(beneficiary);
        return Task.CompletedTask;
    }
}

using MiniBank.Domain.BuildingBlocks;

namespace MiniBank.Infrastructure.Persistence;

/// <summary>DI wrapper for <see cref="MiniBankDbContext"/> as <see cref="IUnitOfWork"/>.</summary>
internal sealed class EfUnitOfWork(MiniBankDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}

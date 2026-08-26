namespace MiniBank.Domain.BuildingBlocks;

/// <summary>
/// Abstraction over the persistence unit-of-work — implemented by the EF Core DbContext.
/// Handlers orchestrate aggregates through repositories and commit once, atomically.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

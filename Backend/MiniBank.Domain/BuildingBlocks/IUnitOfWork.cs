namespace MiniBank.Domain.BuildingBlocks;

/// <summary>Unit-of-work abstraction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Detaches all tracked entities from the change tracker, enabling fresh reloads after concurrency conflicts.</summary>
    void DetachAll();
}

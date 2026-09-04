using MiniBank.Domain.DocumentAggregate.ValueObjects;

namespace MiniBank.Domain.DocumentAggregate;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(DocumentId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(Document document, CancellationToken cancellationToken = default);
}

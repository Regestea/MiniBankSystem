using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.DocumentAggregate;
using MiniBank.Domain.DocumentAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Repositories;

internal sealed class DocumentRepository(MiniBankDbContext db) : IDocumentRepository
{
    public Task<Document?> GetByIdAsync(DocumentId id, CancellationToken cancellationToken = default)
        => db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Document>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        => await db.Documents
            .Where(d => d.CustomerId == customerId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
        => await db.Documents.AddAsync(document, cancellationToken);
}

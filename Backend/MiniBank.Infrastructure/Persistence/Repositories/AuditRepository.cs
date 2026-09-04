using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.AuditAggregate;
using MiniBank.Domain.AuditAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Repositories;

internal sealed class AuditRepository(MiniBankDbContext db) : IAuditRepository
{
    public Task<AuditLog?> GetByIdAsync(AuditLogId id, CancellationToken cancellationToken = default)
        => db.AuditLogs.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        => await db.AuditLogs.AddAsync(auditLog, cancellationToken);
}

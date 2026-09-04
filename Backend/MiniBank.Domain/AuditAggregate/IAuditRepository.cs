using MiniBank.Domain.AuditAggregate.ValueObjects;

namespace MiniBank.Domain.AuditAggregate;

public interface IAuditRepository
{
    Task<AuditLog?> GetByIdAsync(AuditLogId id, CancellationToken cancellationToken = default);
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
}

using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Audit.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    string? EntityType,
    string? EntityId,
    Guid? UserId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page = 1,
    int PageSize = 20
) : IQuery<GetAuditLogsResponse>;

public sealed class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
{
    public GetAuditLogsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.EntityType).MaximumLength(100).When(x => x.EntityType is not null);
        RuleFor(x => x.EntityId).MaximumLength(100).When(x => x.EntityId is not null);
    }
}

public sealed record GetAuditLogsResponse(
    IReadOnlyList<AuditLogItem> Items,
    int TotalCount);

public sealed record AuditLogItem(
    Guid AuditId,
    Guid UserId,
    string UserEmail,
    string Action,
    string EntityType,
    string EntityId,
    string? OldValues,
    string? NewValues,
    string? Description,
    string? IpAddress,
    DateTimeOffset CreatedAt);

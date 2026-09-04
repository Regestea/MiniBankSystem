using Dapper;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Audit.GetAuditLogs;

internal sealed class GetAuditLogsHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<GetAuditLogsQuery, GetAuditLogsResponse>
{
    private const string CountSql = """
        SELECT COUNT(*)
        FROM   audit_logs
        WHERE  (@EntityType IS NULL OR entity_type = @EntityType)
          AND  (@EntityId IS NULL OR entity_id = @EntityId)
          AND  (@UserId IS NULL OR user_id = @UserId)
          AND  (@From IS NULL OR created_at >= @From)
          AND  (@To IS NULL OR created_at <= @To)
        """;

    private const string DataSql = """
        SELECT audit_id        AS AuditId,
               user_id         AS UserId,
               user_email      AS UserEmail,
               action          AS Action,
               entity_type     AS EntityType,
               entity_id       AS EntityId,
               old_values      AS OldValues,
               new_values      AS NewValues,
               description     AS Description,
               ip_address      AS IpAddress,
               created_at      AS CreatedAt
        FROM   audit_logs
        WHERE  (@EntityType IS NULL OR entity_type = @EntityType)
          AND  (@EntityId IS NULL OR entity_id = @EntityId)
          AND  (@UserId IS NULL OR user_id = @UserId)
          AND  (@From IS NULL OR created_at >= @From)
          AND  (@To IS NULL OR created_at <= @To)
        ORDER BY created_at DESC
        LIMIT @PageSize OFFSET @Offset
        """;

    public async Task<GetAuditLogsResponse> HandleAsync(GetAuditLogsQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var offset = (query.Page - 1) * query.PageSize;
        var parameters = new
        {
            query.EntityType,
            query.EntityId,
            query.UserId,
            query.From,
            query.To,
            query.PageSize,
            Offset = offset
        };

        var totalCount = await connection.QuerySingleAsync<int>(
            new CommandDefinition(CountSql, parameters, cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<AuditRow>(
            new CommandDefinition(DataSql, parameters, cancellationToken: cancellationToken));

        var items = rows.Select(r => new AuditLogItem(
            r.AuditId, r.UserId, r.UserEmail, r.Action.ToString(),
            r.EntityType, r.EntityId, r.OldValues, r.NewValues,
            r.Description, r.IpAddress, r.CreatedAt)).ToList();

        return new GetAuditLogsResponse(items, totalCount);
    }

    private sealed record AuditRow(
        Guid AuditId, Guid UserId, string UserEmail, short Action,
        string EntityType, string EntityId, string? OldValues,
        string? NewValues, string? Description, string? IpAddress,
        DateTimeOffset CreatedAt);
}

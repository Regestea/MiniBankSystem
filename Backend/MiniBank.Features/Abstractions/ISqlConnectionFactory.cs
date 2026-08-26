using System.Data;

namespace MiniBank.Features.Abstractions;

/// <summary>
/// Port for the read side (Dapper). Implemented in Infrastructure over Npgsql,
/// using the same connection string that Aspire injects for the write side.
/// </summary>
public interface ISqlConnectionFactory
{
    IDbConnection CreateOpenConnection();
}

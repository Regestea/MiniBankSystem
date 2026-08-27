using System.Data;

namespace MiniBank.Abstractions;

/// <summary>Abstraction for creating open DB connections for Dapper read models.</summary>
public interface ISqlConnectionFactory
{
    IDbConnection CreateOpenConnection();
}

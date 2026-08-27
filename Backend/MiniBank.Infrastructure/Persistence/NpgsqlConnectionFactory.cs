using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using MiniBank.Features.Abstractions;

namespace MiniBank.Infrastructure.Persistence;

/// <summary>Dapper connection factory — same PostgreSQL as EF Core.</summary>
internal sealed class NpgsqlConnectionFactory(IConfiguration configuration)
    : ISqlConnectionFactory
{
    static NpgsqlConnectionFactory() => DefaultTypeMap.MatchNamesWithUnderscores = true;

    public IDbConnection CreateOpenConnection()
    {
        var connectionString = configuration.GetConnectionString("minibankdb")
            ?? throw new InvalidOperationException(
                "Connection string 'minibankdb' not found. Ensure AppHost wires .WithReference(postgresDb).");

        var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        return connection;
    }
}

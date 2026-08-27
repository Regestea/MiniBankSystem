using System.Data;

namespace MiniBank.Features.Abstractions;

public interface ISqlConnectionFactory
{
    IDbConnection CreateOpenConnection();
}

using Dapper;
using MiniBank.Features.Abstractions;
using MiniBank.Features.Customers.GetCustomer;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.GetCurrentCustomer;

internal sealed class GetCurrentCustomerHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<GetCurrentCustomerQuery, CustomerDetailResponse?>
{
    private const string Sql = """
        SELECT c.customer_id, c.full_name, c.email, c.phone_number, c.status, c.created_at
        FROM   customers c
        JOIN   "AspNetUsers" u ON u.customer_id = c.customer_id
        WHERE  u.Id = @UserId
        """;

    public async Task<CustomerDetailResponse?> HandleAsync(GetCurrentCustomerQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        return await connection.QuerySingleOrDefaultAsync<CustomerDetailResponse>(
            new CommandDefinition(Sql, new { UserId = query.UserId }, cancellationToken: cancellationToken));
    }
}

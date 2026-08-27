using Dapper;
using MiniBank.Features.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.GetCustomer;

internal sealed class GetCustomerHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<GetCustomerQuery, CustomerDetailResponse>
{
    private const string Sql = """
        SELECT customer_id, full_name, email, phone_number, status, created_at
        FROM   customers
        WHERE  customer_id = @CustomerId
        """;

    public async Task<CustomerDetailResponse> HandleAsync(GetCustomerQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var customer = await connection.QuerySingleOrDefaultAsync<CustomerDetailResponse>(
            new CommandDefinition(Sql, new { query.CustomerId }, cancellationToken: cancellationToken));

        return customer ?? throw new Domain.BuildingBlocks.Exceptions.NotFoundException("customer", query.CustomerId);
    }
}

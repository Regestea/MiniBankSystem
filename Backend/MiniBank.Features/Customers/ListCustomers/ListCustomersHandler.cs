using Dapper;
using MiniBank.Abstractions;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.ListCustomers;

internal sealed class ListCustomersHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<ListCustomersQuery, CustomersPageResponse>
{
    private const string Sql = """
        SELECT customer_id, full_name, email, phone_number, status, created_at
        FROM   customers
        ORDER  BY created_at DESC
        OFFSET @Offset LIMIT @Limit;

        SELECT COUNT(*) FROM customers;
        """;

    public async Task<CustomersPageResponse> HandleAsync(ListCustomersQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        using var connection = connectionFactory.CreateOpenConnection();

        await using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            Sql,
            new { Offset = (page - 1) * pageSize, Limit = pageSize },
            cancellationToken: cancellationToken));

        var items = (await multi.ReadAsync<CustomerListItemResponse>()).ToList();
        var total = await multi.ReadSingleAsync<int>();

        return new CustomersPageResponse(items, page, pageSize, total);
    }
}

using Dapper;
using MiniBank.Abstractions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Features.Customers.GetCustomer;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.GetCurrentCustomer;

/// <summary>Returns the authenticated caller's own profile — identity comes from the token, never from input.</summary>
internal sealed class GetCurrentCustomerHandler(ISqlConnectionFactory connectionFactory, ICurrentUserContext currentUser)
    : IQueryHandler<GetCurrentCustomerQuery, CustomerDetailResponse?>
{
    private const string Sql = """
        SELECT c.customer_id, c.full_name, c.email, c.phone_number, c.status, c.created_at
        FROM   customers c
        WHERE  c.customer_id = @UserId
        """;

    public async Task<CustomerDetailResponse?> HandleAsync(GetCurrentCustomerQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var row = await connection.QuerySingleOrDefaultAsync<CustomerDetailRow>(
            new CommandDefinition(Sql, new { UserId = currentUser.UserId }, cancellationToken: cancellationToken));

        return row is null
            ? null
            : new CustomerDetailResponse(row.CustomerId, row.FullName, row.Email, row.PhoneNumber,
                                         ((CustomerStatus)row.Status).ToString(), row.CreatedAt);
    }

    private sealed record CustomerDetailRow(
        Guid CustomerId, string FullName, string Email, string PhoneNumber, short Status, DateTimeOffset CreatedAt);
}

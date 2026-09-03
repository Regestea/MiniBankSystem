using Dapper;
using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.GetCustomer;

/// <summary>
/// Returns a customer profile. The caller may only read their own profile;
/// admins may read any. (Defense in depth — the API layer guards too.)
/// </summary>
internal sealed class GetCustomerHandler(ISqlConnectionFactory connectionFactory, ICurrentUserContext currentUser)
    : IQueryHandler<GetCustomerQuery, CustomerDetailResponse>
{
    private const string Sql = """
        SELECT customer_id, full_name, email, phone_number, status, created_at
        FROM   customers
        WHERE  customer_id = @CustomerId
        """;

    public async Task<CustomerDetailResponse> HandleAsync(GetCustomerQuery query, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAdmin && query.CustomerId != currentUser.UserId)
            throw new ForbiddenException("customer", "Customer profile is not owned by the current user.");

        using var connection = connectionFactory.CreateOpenConnection();

        var row = await connection.QuerySingleOrDefaultAsync<CustomerDetailRow>(
            new CommandDefinition(Sql, new { query.CustomerId }, cancellationToken: cancellationToken));

        return row is null
            ? throw new NotFoundException("customer", query.CustomerId)
            : new CustomerDetailResponse(row.CustomerId, row.FullName, row.Email, row.PhoneNumber,
                                         ((CustomerStatus)row.Status).ToString(), row.CreatedAt);
    }

    private sealed record CustomerDetailRow(
        Guid CustomerId, string FullName, string Email, string PhoneNumber, short Status, DateTimeOffset CreatedAt);
}

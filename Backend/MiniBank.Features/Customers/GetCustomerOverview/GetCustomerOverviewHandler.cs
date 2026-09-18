using Dapper;
using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.Ledger;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.GetCustomerOverview;

internal sealed class GetCustomerOverviewHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUserContext currentUser) : IQueryHandler<GetCustomerOverviewQuery, CustomerOverviewResponse>
{
    private static readonly int[] CreditTypes =
        [(int)LedgerEntryType.Deposit, (int)LedgerEntryType.TransferIn];

    private const string CustomerSql = """
        SELECT customer_id, full_name, email, phone_number, status, created_at
        FROM   customers
        WHERE  customer_id = @UserId
        """;

    private const string AccountsSql = """
        SELECT a.account_id,
               a.account_number,
               a.account_type,
               a.status,
               COALESCE(SUM(CASE WHEN e.type = ANY(@CreditTypes) THEN e.amount ELSE -e.amount END), 0) AS balance,
               a.created_at
        FROM   accounts a
        LEFT   JOIN ledger_entries e ON e.account_id = a.account_id
        WHERE  a.customer_id = @UserId
        GROUP  BY a.account_id, a.account_number, a.account_type, a.status, a.created_at
        ORDER  BY a.created_at
        """;

    public async Task<CustomerOverviewResponse> HandleAsync(GetCustomerOverviewQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateOpenConnection();

        var customer = await connection.QuerySingleOrDefaultAsync<CustomerRow>(
            new CommandDefinition(CustomerSql, new { UserId = currentUser.UserId }, cancellationToken: cancellationToken));

        if (customer is null)
            throw new Domain.BuildingBlocks.Exceptions.NotFoundException("customer", currentUser.UserId);

        var accounts = (await connection.QueryAsync<AccountRow>(
            new CommandDefinition(AccountsSql, new { UserId = currentUser.UserId, CreditTypes }, cancellationToken: cancellationToken))).ToList();

        var dtos = accounts.Select(a => new OverviewAccountDto(
            a.AccountId, a.AccountNumber,
            ((AccountType)a.AccountType).ToString(),
            ((AccountStatus)a.Status).ToString(),
            a.Balance, DbTime.Utc(a.CreatedAt))).ToList();

        return new CustomerOverviewResponse(
            customer.CustomerId, customer.FullName, customer.Email, customer.PhoneNumber,
            ((CustomerStatus)customer.Status).ToString(), DbTime.Utc(customer.CreatedAt),
            dtos, dtos.Sum(a => a.Balance));
    }

    private sealed record CustomerRow(
        Guid CustomerId, string FullName, string Email, string PhoneNumber, short Status, DateTime CreatedAt);

    private sealed record AccountRow(
        Guid AccountId, string AccountNumber, short AccountType, short Status, decimal Balance, DateTime CreatedAt);
}

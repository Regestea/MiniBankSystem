using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.ListCustomers;

public sealed record ListCustomersQuery(int Page = 1, int PageSize = 20) : IQuery<CustomersPageResponse>;

public sealed record CustomersPageResponse(
    IReadOnlyList<CustomerListItemResponse> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record CustomerListItemResponse(
    Guid CustomerId,
    string FullName,
    string Email,
    string PhoneNumber,
    string Status,
    DateTimeOffset CreatedAt);

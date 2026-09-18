using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.GetCustomerOverview;

/// <summary>
/// Single-page customer overview: own identity (full name, email, phone) plus
/// every account number with its balance and the total balance across accounts.
/// Self-contained DTOs (no cross-slice reference to Accounts).
/// </summary>
public sealed record GetCustomerOverviewQuery : IQuery<CustomerOverviewResponse>;

public sealed record CustomerOverviewResponse(
    Guid CustomerId,
    string FullName,
    string Email,
    string PhoneNumber,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OverviewAccountDto> Accounts,
    decimal TotalBalance);

public sealed record OverviewAccountDto(
    Guid AccountId,
    string AccountNumber,
    string AccountType,
    string Status,
    decimal Balance,
    DateTimeOffset CreatedAt);

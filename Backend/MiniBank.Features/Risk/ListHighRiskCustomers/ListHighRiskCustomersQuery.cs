using MiniBank.Features.Messaging;

namespace MiniBank.Features.Risk.ListHighRiskCustomers;

public sealed record ListHighRiskCustomersQuery(int? MinScore) : IQuery<ListHighRiskCustomersResponse>;

public sealed record ListHighRiskCustomersResponse(
    IReadOnlyList<HighRiskCustomerItem> Customers);

public sealed record HighRiskCustomerItem(
    Guid RiskId,
    Guid CustomerId,
    string RiskLevel,
    decimal DailyTransactionLimit,
    int TransactionsToday,
    decimal AmountToday);

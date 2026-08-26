using MiniBank.Features.Messaging;

namespace MiniBank.Features.Reports.GetBankReport;

public sealed record GetBankReportQuery : IQuery<BankReportResponse>;

public sealed record BankReportResponse(
    int Customers,
    int Accounts,
    int ActiveAccounts,
    decimal TotalBalance);

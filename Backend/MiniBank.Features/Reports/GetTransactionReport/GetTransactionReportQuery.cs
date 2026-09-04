using MiniBank.Features.Messaging;

namespace MiniBank.Features.Reports.GetTransactionReport;

public sealed record GetTransactionReportQuery(
    DateTimeOffset? From,
    DateTimeOffset? To
) : IQuery<TransactionReportResponse>;

public sealed record TransactionReportResponse(
    int TotalTransactions,
    decimal TotalDeposits,
    decimal TotalWithdrawals,
    decimal TotalTransfers,
    int TransactionsToday,
    decimal VolumeToday);

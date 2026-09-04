using MiniBank.Features.Messaging;

namespace MiniBank.Features.Reports.GetCustomerReport;

public sealed record GetCustomerReportQuery : IQuery<CustomerReportResponse>;

public sealed record CustomerReportResponse(
    int TotalCustomers,
    int PendingCustomers,
    int VerifiedCustomers,
    int BlockedCustomers,
    int KycPending,
    int KycSubmitted,
    int KycApproved,
    int KycRejected);

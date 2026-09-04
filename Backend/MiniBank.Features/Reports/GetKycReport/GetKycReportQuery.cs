using MiniBank.Features.Messaging;

namespace MiniBank.Features.Reports.GetKycReport;

public sealed record GetKycReportQuery : IQuery<KycReportResponse>;

public sealed record KycReportResponse(
    int TotalVerifications,
    int Pending,
    int Submitted,
    int Approved,
    int Rejected,
    int DocumentsUploaded,
    int DocumentsVerified,
    int DocumentsRejected);

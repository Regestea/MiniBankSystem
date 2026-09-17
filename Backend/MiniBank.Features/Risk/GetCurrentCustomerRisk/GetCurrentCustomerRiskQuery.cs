using MiniBank.Features.Messaging;
using MiniBank.Features.Risk.GetCustomerRisk;

namespace MiniBank.Features.Risk.GetCurrentCustomerRisk;

/// <summary>
/// Returns the authenticated caller's own risk info — no CustomerId in the contract.
/// </summary>
public sealed record GetCurrentCustomerRiskQuery : IQuery<GetCustomerRiskResponse>;

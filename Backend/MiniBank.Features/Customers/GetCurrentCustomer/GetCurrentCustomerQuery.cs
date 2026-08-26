using MiniBank.Features.Customers.GetCustomer;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.GetCurrentCustomer;

public sealed record GetCurrentCustomerQuery(Guid UserId) : IQuery<CustomerDetailResponse?>;

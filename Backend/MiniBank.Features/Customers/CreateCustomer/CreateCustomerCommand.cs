using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.CreateCustomer;

public sealed record CreateCustomerCommand(
    string FullName,
    string Email,
    string PhoneNumber) : ICommand<CustomerResponse>;

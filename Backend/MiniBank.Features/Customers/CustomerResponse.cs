using MiniBank.Domain.CustomerAggregate;

namespace MiniBank.Features.Customers;

public sealed record CustomerResponse(
    Guid CustomerId,
    string FullName,
    string Email,
    string PhoneNumber,
    string Status,
    DateTimeOffset CreatedAt)
{
    public static CustomerResponse From(Customer customer)
        => new(customer.Id.Value, customer.FullName, customer.Email, customer.PhoneNumber,
               customer.Status.ToString(), customer.CreatedAt);
}

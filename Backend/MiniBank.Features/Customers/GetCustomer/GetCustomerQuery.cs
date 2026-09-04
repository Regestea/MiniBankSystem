using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.GetCustomer;

public sealed record GetCustomerQuery(Guid CustomerId) : IQuery<CustomerDetailResponse>;

public sealed class GetCustomerQueryValidator : AbstractValidator<GetCustomerQuery>
{
    public GetCustomerQueryValidator()
        => RuleFor(x => x.CustomerId).NotEmpty();
}

public sealed record CustomerDetailResponse(
    Guid CustomerId,
    string FullName,
    string Email,
    string PhoneNumber,
    string Status,
    DateTimeOffset CreatedAt);

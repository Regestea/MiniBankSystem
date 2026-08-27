using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Abstractions;
using MiniBank.Features.Customers;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.LinkCustomer;

/// <summary>Links authenticated user to new customer profile.</summary>
internal sealed class LinkCustomerHandler(
    IAppUserDirectory users,
    ICustomerRepository customers,
    IUnitOfWork unitOfWork) : ICommandHandler<LinkCustomerCommand, CustomerResponse>
{
    public async Task<CustomerResponse> HandleAsync(LinkCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByIdAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException("user", command.UserId);

        if (user.CustomerId is not null)
            throw new DomainConflictException(nameof(command.UserId), "User already linked to a customer.");

        if (string.IsNullOrWhiteSpace(user.Email))
            throw new DomainValidationException(nameof(user.Email), "Identity user has no email.");

        var email = new Email(user.Email);
        if (await customers.EmailExistsAsync(email, cancellationToken))
            throw new DomainConflictException(nameof(email), "Email already registered as a customer.");

        var customer = Customer.Create(command.FullName, email, command.PhoneNumber);

        await customers.AddAsync(customer, cancellationToken);

        if (!await users.TryAttachCustomerAsync(command.UserId, customer.Id.Value, cancellationToken))
            throw new NotFoundException("user", command.UserId);

        await users.EnsureUserRoleAsync(command.UserId, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerResponse.From(customer);
    }
}

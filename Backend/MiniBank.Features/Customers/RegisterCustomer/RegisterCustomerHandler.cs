using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Features.Customers;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Customers.RegisterCustomer;

/// <summary>
/// Atomically registers a new customer: creates both the ASP.NET Identity user
/// and the Customer aggregate using a single shared Guid. Both entities are staged
/// in the EF Core change tracker and persisted in a single SaveChangesAsync call,
/// ensuring an atomic transaction.
/// </summary>
internal sealed class RegisterCustomerHandler(
    ICustomerRepository customers,
    IIdentityUserService identityUsers,
    IUnitOfWork unitOfWork) : ICommandHandler<RegisterCustomerCommand, CustomerResponse>
{
    public async Task<CustomerResponse> HandleAsync(RegisterCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customerId = new CustomerId(Guid.NewGuid());

        var email = new Email(command.Email);

        if (await customers.EmailExistsAsync(email, cancellationToken))
            throw new DomainConflictException(nameof(email), "Email already registered as a customer.");

        // Stage IdentityUser creation (UserManager.CreateAsync does NOT call SaveChanges)
        await identityUsers.CreateUserAsync(customerId.Value, command.Email, command.Password, cancellationToken);

        // Create Customer aggregate with the same Guid (1:1 same-Guid invariant)
        var customer = Customer.Create(command.FullName, email, command.PhoneNumber, customerId);

        await customers.AddAsync(customer, cancellationToken);

        // Persist "User" role for future tokens
        await identityUsers.EnsureUserRoleAsync(customerId.Value, cancellationToken);

        // Single SaveChangesAsync persists both IdentityUser + Customer atomically
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerResponse.From(customer);
    }
}

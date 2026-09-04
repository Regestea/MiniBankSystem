using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Domain.RiskAggregate;
using MiniBank.Features.Customers;
using MiniBank.Features.Messaging;
using Microsoft.Extensions.Logging;

namespace MiniBank.Features.Customers.RegisterCustomer;

/// <summary>
/// Registers a new customer in two phases (Identity persists immediately via UserManager,
/// then Customer + CustomerRisk persist in one SaveChanges). Shares one Guid across all three.
/// If the second phase fails, compensates by deleting the orphan IdentityUser.
/// </summary>
internal sealed class RegisterCustomerHandler(
    ICustomerRepository customers,
    IRiskRepository riskRepo,
    IIdentityUserService identityUsers,
    IUnitOfWork unitOfWork,
    ILogger<RegisterCustomerHandler> logger) : ICommandHandler<RegisterCustomerCommand, CustomerResponse>
{
    public async Task<CustomerResponse> HandleAsync(RegisterCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customerId = new CustomerId(Guid.NewGuid());

        var email = new Email(command.Email);

        if (await customers.EmailExistsAsync(email, cancellationToken))
            throw new DomainConflictException(nameof(email), "Email already registered as a customer.");

        await identityUsers.CreateUserAsync(customerId.Value, command.Email, command.Password, cancellationToken);

        var customer = Customer.Create(command.FullName, email, command.PhoneNumber, customerId);

        await customers.AddAsync(customer, cancellationToken);

        var risk = CustomerRisk.Create(customerId.Value);
        await riskRepo.AddAsync(risk, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Phase 2 failed after phase 1 committed: remove the orphan IdentityUser (best-effort).
            try { await identityUsers.DeleteUserAsync(customerId.Value, cancellationToken); }
            catch (Exception ex)
            {
                logger.LogError(ex, "Compensation failed: orphan IdentityUser {CustomerId} may remain.", customerId.Value);
            }
            throw;
        }

        // Role assignment is outside the atomic transaction but has a fallback:
        // UserRoleClaimsTransformation grants "User" claim implicitly when no role claims exist.
        try
        {
            await identityUsers.EnsureUserRoleAsync(customerId.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to assign role to user {CustomerId} after registration. " +
                "UserRoleClaimsTransformation will provide fallback access.", customerId.Value);
        }

        return CustomerResponse.From(customer);
    }
}

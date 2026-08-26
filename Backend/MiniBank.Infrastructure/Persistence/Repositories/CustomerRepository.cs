using Microsoft.EntityFrameworkCore;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository(MiniBankDbContext db) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken = default)
        => db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        => await db.Customers.AddAsync(customer, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => db.Customers.AnyAsync(c => c.Email == email, cancellationToken);
}

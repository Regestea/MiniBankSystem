using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Infrastructure.Identity;

namespace MiniBank.Infrastructure.Persistence.Configurations;

/// <summary>
/// Links identity users to the domain Customer aggregate.
/// One-way FK only (no navigation) — Infrastructure may know Domain ids; Domain never knows Identity.
/// </summary>
internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.Property(u => u.CustomerId)
            .HasColumnName("customer_id");
        b.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(u => u.CustomerId)
            .HasConstraintName("fk_users_customer")
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(u => u.CustomerId)
            .HasDatabaseName("ix_users_customer");
    }
}

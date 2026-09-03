using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("customers");

        b.HasKey(c => c.Id).HasName("pk_customers");
        b.Property(c => c.Id)
            .HasColumnName("customer_id")
            .HasConversion(id => id.Value, g => new CustomerId(g))
            .ValueGeneratedNever();

        b.Property(c => c.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(100)
            .IsRequired()
            .HasConversion(n => (string)n, v => new FullName(v));

        b.Property(c => c.Email)
            .HasColumnName("email")
            .HasMaxLength(254)
            .IsRequired()
            .HasConversion(e => (string)e, v => new Email(v));

        b.Property(c => c.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(15)
            .IsRequired()
            .HasConversion(p => (string)p, v => new PhoneNumber(v));

        b.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();

        b.Property(c => c.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        b.Property(c => c.CreatedAt).HasColumnName("created_at");
        b.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(c => c.Email).IsUnique().HasDatabaseName("ux_customers_email");

        // NOTE: FK customers.customer_id -> AspNetUsers.Id is added as a raw DEFERRABLE
        // constraint in migration AddCustomerIdentityFk. An EF model relationship is not
        // possible here because the typed CustomerId key cannot match the Guid principal key.
    }
}

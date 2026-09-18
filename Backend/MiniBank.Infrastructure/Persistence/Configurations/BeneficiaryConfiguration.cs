using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BeneficiaryAggregate;
using MiniBank.Domain.BeneficiaryAggregate.ValueObjects;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Configurations;

internal sealed class BeneficiaryConfiguration : IEntityTypeConfiguration<Beneficiary>
{
    public void Configure(EntityTypeBuilder<Beneficiary> b)
    {
        b.ToTable("beneficiaries");

        b.HasKey(x => x.Id).HasName("pk_beneficiaries");
        b.Property(x => x.Id)
            .HasColumnName("beneficiary_id")
            .HasConversion(id => id.Value, g => new BeneficiaryId(g))
            .ValueGeneratedNever();

        b.Property(x => x.OwnerCustomerId)
            .HasColumnName("owner_customer_id")
            .IsRequired()
            .HasConversion(id => id.Value, g => new CustomerId(g));

        b.Property(x => x.AccountNumber)
            .HasColumnName("account_number")
            .HasMaxLength(16)
            .IsRequired()
            .HasConversion(n => (string)n, v => new AccountNumber(v));

        b.Property(x => x.HolderName)
            .HasColumnName("holder_name")
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(x => new { x.OwnerCustomerId, x.AccountNumber })
            .IsUnique()
            .HasDatabaseName("ux_beneficiaries_owner_number");
        b.HasIndex(x => x.OwnerCustomerId).HasDatabaseName("ix_beneficiaries_owner");
    }
}

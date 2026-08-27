using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        b.ToTable("accounts");

        b.HasKey(a => a.Id).HasName("pk_accounts");
        b.Property(a => a.Id)
            .HasColumnName("account_id")
            .HasConversion(id => id.Value, g => new AccountId(g))
            .ValueGeneratedNever();

        b.Property(a => a.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired()
            .HasConversion(id => id.Value, g => new CustomerId(g));

        b.Property(a => a.AccountNumber)
            .HasColumnName("account_number")
            .HasMaxLength(16)
            .IsRequired()
            .HasConversion(n => (string)n, v => new AccountNumber(v));

        b.Property(a => a.AccountType)
            .HasColumnName("account_type")
            .HasConversion<short>()
            .IsRequired();

        b.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();

        b.Property(a => a.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        b.Property(a => a.CreatedAt).HasColumnName("created_at");
        b.Property(a => a.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(a => a.AccountNumber).IsUnique().HasDatabaseName("ux_accounts_number");
        b.HasIndex(a => a.CustomerId).HasDatabaseName("ix_accounts_customer");

        b.OwnsMany(a => a.Ledger, lb =>
        {
            lb.ToTable("ledger_entries");

            lb.WithOwner().HasForeignKey(e => e.AccountId);

            lb.Property(e => e.Id)
                .HasColumnName("ledger_entry_id")
                .ValueGeneratedNever();

            lb.Property(e => e.AccountId)
                .HasColumnName("account_id")
                .HasConversion(id => id.Value, g => new AccountId(g));

            lb.Property(e => e.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .HasConversion(m => m.Amount, v => Money.FromDecimal(v))
                .IsRequired();

            lb.Property(e => e.Type)
                .HasColumnName("type")
                .HasConversion<short>()
                .IsRequired();

            lb.Property(e => e.OccurredOn).HasColumnName("occurred_on").IsRequired();
            lb.Property(e => e.ReferenceId).HasColumnName("reference_id").HasMaxLength(64);
            lb.Property(e => e.Description).HasColumnName("description").HasMaxLength(200);

            lb.HasIndex(e => new { e.AccountId, e.OccurredOn, e.Id })
              .HasDatabaseName("ix_ledger_account_time");
            lb.HasIndex(e => e.ReferenceId).HasDatabaseName("ix_ledger_reference");
        });

        b.Navigation(a => a.Ledger).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

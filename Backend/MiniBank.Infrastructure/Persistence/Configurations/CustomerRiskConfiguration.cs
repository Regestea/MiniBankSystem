using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniBank.Domain.RiskAggregate;
using MiniBank.Domain.RiskAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Configurations;

internal sealed class CustomerRiskConfiguration : IEntityTypeConfiguration<CustomerRisk>
{
    public void Configure(EntityTypeBuilder<CustomerRisk> b)
    {
        b.ToTable("customer_risks");

        b.HasKey(r => r.Id).HasName("pk_customer_risks");
        b.Property(r => r.Id)
            .HasColumnName("risk_id")
            .HasConversion(id => id.Value, g => new CustomerRiskId(g))
            .ValueGeneratedNever();

        b.Property(r => r.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        b.Property(r => r.RiskLevel)
            .HasColumnName("risk_level")
            .HasConversion<short>()
            .IsRequired();

        b.Property(r => r.DailyTransactionLimit)
            .HasColumnName("daily_transaction_limit")
            .HasPrecision(18, 2)
            .IsRequired();

        b.Property(r => r.DailyTransactionCountLimit)
            .HasColumnName("daily_transaction_count_limit")
            .IsRequired();

        b.Property(r => r.TransactionsToday)
            .HasColumnName("transactions_today")
            .IsRequired();

        b.Property(r => r.AmountToday)
            .HasColumnName("amount_today")
            .HasPrecision(18, 2)
            .IsRequired();

        b.Property(r => r.LastResetDate)
            .HasColumnName("last_reset_date")
            .IsRequired();

        b.Property(r => r.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        b.Property(r => r.CreatedAt).HasColumnName("created_at");
        b.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(r => r.CustomerId).IsUnique().HasDatabaseName("ux_risk_customer");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniBank.Domain.AccountAggregate;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks.ValueObjects;
using MiniBank.Domain.TransactionAggregate;
using MiniBank.Domain.TransactionAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        b.ToTable("transactions", t =>
        {
            // Transfer ⇒ both sides set · Deposit/Withdraw ⇒ exactly one side set
            t.HasCheckConstraint("ck_transactions_sides",
                "(type = 2 AND source_account_id IS NOT NULL AND destination_account_id IS NOT NULL) " +
                "OR (type IN (0,1) AND (source_account_id IS NULL) <> (destination_account_id IS NULL))");

            t.HasCheckConstraint("ck_transactions_amount_positive", "amount > 0");
        });

        b.HasKey(x => x.Id).HasName("pk_transactions");
        b.Property(x => x.Id)
            .HasColumnName("transaction_id")
            .HasConversion(id => id.Value, g => new TransactionId(g))
            .ValueGeneratedNever();

        b.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<short>()
            .IsRequired();

        b.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .HasConversion(m => m.Amount, v => Money.FromDecimal(v))
            .IsRequired();

        b.Property(x => x.SourceAccountId)
            .HasColumnName("source_account_id")
            .HasConversion(id => id!.Value, g => new AccountId(g));

        b.Property(x => x.DestinationAccountId)
            .HasColumnName("destination_account_id")
            .HasConversion(id => id!.Value, g => new AccountId(g));

        b.Property(x => x.OccurredOn).HasColumnName("occurred_on").IsRequired();
        b.Property(x => x.ReferenceId).HasColumnName("reference_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(200);

        b.Property(x => x.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        // Postings are derived/cached in the domain and persisted under accounts.ledger_entries
        b.Ignore(x => x.Postings);

        b.HasIndex(x => x.ReferenceId).IsUnique().HasDatabaseName("ux_transactions_reference");

        b.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.SourceAccountId)
            .HasConstraintName("fk_transactions_source_account")
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.DestinationAccountId)
            .HasConstraintName("fk_transactions_destination_account")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

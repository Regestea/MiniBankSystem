using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniBank.Domain.KycAggregate;
using MiniBank.Domain.KycAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Configurations;

internal sealed class KycVerificationConfiguration : IEntityTypeConfiguration<KycVerification>
{
    public void Configure(EntityTypeBuilder<KycVerification> b)
    {
        b.ToTable("kyc_verifications");

        b.HasKey(k => k.Id).HasName("pk_kyc_verifications");
        b.Property(k => k.Id)
            .HasColumnName("kyc_id")
            .HasConversion(id => id.Value, g => new KycVerificationId(g))
            .ValueGeneratedNever();

        b.Property(k => k.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        b.Property(k => k.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();

        b.Property(k => k.PrimaryDocumentId)
            .HasColumnName("primary_document_id");

        b.Property(k => k.SubmittedAt)
            .HasColumnName("submitted_at");

        b.Property(k => k.ReviewedAt)
            .HasColumnName("reviewed_at");

        b.Property(k => k.ReviewedBy)
            .HasColumnName("reviewed_by");

        b.Property(k => k.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(500);

        b.Property(k => k.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        b.Property(k => k.CreatedAt).HasColumnName("created_at");
        b.Property(k => k.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(k => k.CustomerId).IsUnique().HasDatabaseName("ux_kyc_customer");
        // Lookup index for the Approve gate (primary_document_id -> documents).
        // No FK: PrimaryDocumentId is Guid? while Document.Id is DocumentId VO;
        // ownership/existence is enforced in ReviewKycHandler instead.
        b.HasIndex(k => k.PrimaryDocumentId).HasDatabaseName("ix_kyc_primary_document");
    }
}

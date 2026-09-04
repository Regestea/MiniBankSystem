using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniBank.Domain.DocumentAggregate;
using MiniBank.Domain.DocumentAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Configurations;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.ToTable("documents");

        b.HasKey(d => d.Id).HasName("pk_documents");
        b.Property(d => d.Id)
            .HasColumnName("document_id")
            .HasConversion(id => id.Value, g => new DocumentId(g))
            .ValueGeneratedNever();

        b.Property(d => d.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        b.Property(d => d.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        b.Property(d => d.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(100)
            .IsRequired();

        b.Property(d => d.FileSize)
            .HasColumnName("file_size")
            .IsRequired();

        b.Property(d => d.StoragePath)
            .HasColumnName("storage_path")
            .HasMaxLength(500)
            .IsRequired();

        b.Property(d => d.Type)
            .HasColumnName("type")
            .HasConversion<short>()
            .IsRequired();

        b.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();

        b.Property(d => d.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(500);

        b.Property(d => d.VerifiedBy)
            .HasColumnName("verified_by");

        b.Property(d => d.VerifiedAt)
            .HasColumnName("verified_at");

        b.Property(d => d.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        b.Property(d => d.CreatedAt).HasColumnName("created_at");
        b.Property(d => d.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(d => d.CustomerId).HasDatabaseName("ix_documents_customer");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniBank.Domain.AuditAggregate;
using MiniBank.Domain.AuditAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");

        b.HasKey(a => a.Id).HasName("pk_audit_logs");
        b.Property(a => a.Id)
            .HasColumnName("audit_id")
            .HasConversion(id => id.Value, g => new AuditLogId(g))
            .ValueGeneratedNever();

        b.Property(a => a.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        b.Property(a => a.UserEmail)
            .HasColumnName("user_email")
            .HasMaxLength(254)
            .IsRequired();

        b.Property(a => a.Action)
            .HasColumnName("action")
            .HasConversion<short>()
            .IsRequired();

        b.Property(a => a.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(100)
            .IsRequired();

        b.Property(a => a.EntityId)
            .HasColumnName("entity_id")
            .HasMaxLength(100)
            .IsRequired();

        b.Property(a => a.OldValues)
            .HasColumnName("old_values");

        b.Property(a => a.NewValues)
            .HasColumnName("new_values");

        b.Property(a => a.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        b.Property(a => a.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);

        b.Property(a => a.CreatedAt).HasColumnName("created_at");

        b.HasIndex(a => a.EntityType).HasDatabaseName("ix_audit_entity_type");
        b.HasIndex(a => new { a.EntityType, a.EntityId }).HasDatabaseName("ix_audit_entity");
        b.HasIndex(a => a.UserId).HasDatabaseName("ix_audit_user");
        b.HasIndex(a => a.CreatedAt).HasDatabaseName("ix_audit_created");
    }
}

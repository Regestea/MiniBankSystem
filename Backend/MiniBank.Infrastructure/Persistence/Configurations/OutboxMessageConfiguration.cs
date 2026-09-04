using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.ValueObjects;

namespace MiniBank.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new OutboxMessageId(value))
            .HasColumnName("id");

        builder.Property(x => x.EventType)
            .HasMaxLength(200)
            .IsRequired()
            .HasColumnName("event_type");

        builder.Property(x => x.Payload)
            .HasColumnType("jsonb")
            .IsRequired()
            .HasColumnName("payload");

        builder.Property(x => x.OccurredOn)
            .IsRequired()
            .HasColumnName("occurred_on");

        builder.Property(x => x.ProcessedOn)
            .HasColumnName("processed_on");

        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasColumnName("retry_count");

        builder.Property(x => x.Error)
            .HasMaxLength(2000)
            .HasColumnName("error");

        builder.Property(x => x.Version)
            .IsRequired()
            .HasColumnName("version")
            .IsConcurrencyToken();

        builder.HasIndex(x => new { x.ProcessedOn, x.OccurredOn })
            .HasDatabaseName("ix_outbox_messages_processing");

        builder.HasIndex(x => x.EventType)
            .HasDatabaseName("ix_outbox_messages_event_type");
    }
}
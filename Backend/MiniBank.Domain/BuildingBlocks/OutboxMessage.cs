using MiniBank.Domain.BuildingBlocks.ValueObjects;

namespace MiniBank.Domain.BuildingBlocks;

public sealed class OutboxMessage : AggregateRoot<OutboxMessageId>
{
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTimeOffset OccurredOn { get; private set; }
    public DateTimeOffset? ProcessedOn { get; private set; }
    public int RetryCount { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(OutboxMessageId id, string eventType, string payload, DateTimeOffset occurredOn)
        : base(id)
    {
        EventType = eventType;
        Payload = payload;
        OccurredOn = occurredOn;
        RetryCount = 0;
    }

    public static OutboxMessage Create(string eventType, string payload, DateTimeOffset occurredOn, OutboxMessageId? id = null)
        => new(id ?? new OutboxMessageId(Guid.NewGuid()), eventType, payload, occurredOn);

    public void MarkProcessed()
    {
        ProcessedOn = DateTimeOffset.UtcNow;
        IncrementVersion();
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        Error = error;
        IncrementVersion();
    }
}
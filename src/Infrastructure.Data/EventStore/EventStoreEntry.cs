namespace Infrastructure.Data.EventStore;

public class EventStoreEntry
{
    public long Sequence { get; set; }
    public Guid EventId { get; set; }
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
}

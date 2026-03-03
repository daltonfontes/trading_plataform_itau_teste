using System.Text.Json;
using Domain.Events;
using Domain.Events.Base;

namespace Infrastructure.Data.EventStore;

public class EventSerializer
{
    private static readonly Dictionary<string, Type> _types = new()
    {
        [nameof(DistributedContribution)]   = typeof(DistributedContribution),
        [nameof(AssetsSoldRebalancing)]     = typeof(AssetsSoldRebalancing),
    };

    public string Serialize(DomainEvent domainEvent) =>
        JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

    public DomainEvent Deserialize(string eventType, string payload)
    {
        if (!_types.TryGetValue(eventType, out var type))
            throw new InvalidOperationException($"Unknown event type: {eventType}");

        return (DomainEvent)JsonSerializer.Deserialize(payload, type)!;
    }
}

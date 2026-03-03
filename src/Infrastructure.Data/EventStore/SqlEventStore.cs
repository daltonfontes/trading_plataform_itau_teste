using Domain.Events.Base;
using Domain.Interfaces;
using Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.EventStore;

public class SqlEventStore : IEventStore
{
    private readonly DataContext _context;
    private readonly EventSerializer _serializer;

    public SqlEventStore(DataContext context, EventSerializer serializer)
    {
        _context = context;
        _serializer = serializer;
    }

    public async Task AppendAsync(IEnumerable<DomainEvent> events, CancellationToken ct = default)
    {
        foreach (var domainEvent in events)
        {
            _context.EventStoreEntries.Add(new EventStoreEntry
            {
                EventId     = domainEvent.EventId,
                AggregateId = domainEvent.AggregateId,
                EventType   = domainEvent.GetType().Name,
                Payload     = _serializer.Serialize(domainEvent),
                OccurredOn  = domainEvent.OccurredOn
            });
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<DomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken ct = default)
    {
        var entries = await _context.EventStoreEntries
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.Sequence)
            .ToListAsync(ct);

        return entries.Select(e => _serializer.Deserialize(e.EventType, e.Payload));
    }
}

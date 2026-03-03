using Domain.Events.Base;

namespace Domain.Interfaces;

public interface IEventStore
{
    Task AppendAsync(IEnumerable<DomainEvent> events, CancellationToken ct = default);
    Task<IEnumerable<DomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken ct = default);
}

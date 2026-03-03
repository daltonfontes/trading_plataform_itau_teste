
namespace Domain.Events.Base
{
    public abstract record DomainEvent(
        Guid EventId,
        DateTime OccurredOn,
        string TypeAggregates,
        Guid AggregateId
    );
}

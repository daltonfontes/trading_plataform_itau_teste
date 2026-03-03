using Domain.Events.Base;

namespace Domain.Events
{
    public record DistributedContribution
    (
        Guid EventId,
        DateTime OccurredOn,
        Guid CustomerId,
        string Ticker,
        decimal Quantity,
        decimal UnityPrice,
        decimal TotalPrice,
        decimal Tax, // IRDEDODURO
        Guid BuyCicleId
    ) : DomainEvent(EventId, OccurredOn, "CustomerCustody", CustomerId);
}

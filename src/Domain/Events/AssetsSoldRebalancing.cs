using Domain.Events.Base;

namespace Domain.Events
{
    public record AssetsSoldRebalancing
    (
        Guid EventId,
        DateTime OccurredOn,
        Guid CustomerId,
        string Ticker,
        decimal Quantity,
        decimal UnityPrice,
        decimal NetProfit
    ) : DomainEvent(EventId, OccurredOn, "CustomerCustody", CustomerId);
}

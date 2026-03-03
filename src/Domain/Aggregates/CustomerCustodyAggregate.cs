using Domain.Events.Base;
using Domain.Events;

namespace Domain.Aggregates
{
    public class CustomerCustodyAggregate
    {
        public Guid CustomerId { get; set; }
        public Dictionary<string, decimal> Positions { get; private set; } = [];
        public Dictionary<string, decimal> AveragePrice { get; private set; } = [];
        private readonly List<DomainEvent> _events = [];

        public static CustomerCustodyAggregate Recreate(IEnumerable<DomainEvent> events)
        {
            var aggregate = new CustomerCustodyAggregate();
            foreach (var e in events)
            {
                aggregate.CustomerId = e.AggregateId;
                aggregate.Apply(e);
            }
            return aggregate;
        }

        // motor de compra chama isso
        public void RegisterContribuition(string ticker, decimal quantity, decimal price, decimal tax, Guid cicleId)
        {
            var contributionEvent = new DistributedContribution(
                Guid.NewGuid(),
                DateTime.UtcNow,
                CustomerId,
                ticker,
                quantity,
                price,
                quantity * price,
                tax,
                cicleId
            );
            Apply(contributionEvent);
            _events.Add(contributionEvent);
        }

        private void Apply(DomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                case DistributedContribution distributedContribution:
                    var previousQuantity = Positions.GetValueOrDefault(distributedContribution.Ticker, 0m);
                    var previousAveragePrice = AveragePrice.GetValueOrDefault(distributedContribution.Ticker, 0m);
                    Positions[distributedContribution.Ticker] = previousQuantity + distributedContribution.Quantity;
                    AveragePrice[distributedContribution.Ticker] = (previousQuantity * previousAveragePrice
                        + distributedContribution.Quantity * distributedContribution.UnityPrice)
                        / Positions[distributedContribution.Ticker];
                    break;

                case AssetsSoldRebalancing assetsSoldRebalancing:
                    Positions[assetsSoldRebalancing.Ticker] = Positions.GetValueOrDefault(assetsSoldRebalancing.Ticker) - assetsSoldRebalancing.Quantity;
                    break;
            }
        }

        // motor de rebalanceamento chama isso
        public void RegisterSale(string ticker, decimal quantity, decimal unitPrice, decimal netProfit)
        {
            var saleEvent = new AssetsSoldRebalancing(
                Guid.NewGuid(),
                DateTime.UtcNow,
                CustomerId,
                ticker,
                quantity,
                unitPrice,
                netProfit
            );
            Apply(saleEvent);
            _events.Add(saleEvent);
        }

        public IReadOnlyList<DomainEvent> GetEvents() => _events.AsReadOnly();
    }
}

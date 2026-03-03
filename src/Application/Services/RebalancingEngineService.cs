using Application.Events;
using Application.Interfaces;
using Domain.Aggregates;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces;

namespace Application.Services;

public class RebalancingEngineService : IRebalancingEngineService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IAssetPriceRepository _assetPriceRepository;
    private readonly ICustomerCustodyRepository _customerCustodyRepository;
    private readonly IEventStore _eventStore;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IrCalculationService _irCalculationService;
    private readonly decimal _deviationThreshold;
    private readonly string _irRebalancingTopic;

    public RebalancingEngineService(
        ICustomerRepository customerRepository,
        IAssetPriceRepository assetPriceRepository,
        ICustomerCustodyRepository customerCustodyRepository,
        IEventStore eventStore,
        IKafkaProducer kafkaProducer,
        IrCalculationService irCalculationService,
        decimal deviationThreshold,
        string irRebalancingTopic)
    {
        _customerRepository = customerRepository;
        _assetPriceRepository = assetPriceRepository;
        _customerCustodyRepository = customerCustodyRepository;
        _eventStore = eventStore;
        _kafkaProducer = kafkaProducer;
        _irCalculationService = irCalculationService;
        _deviationThreshold = deviationThreshold;
        _irRebalancingTopic = irRebalancingTopic;
    }

    // 4.4A — Basket composition changed
    public async Task RebalanceOnBasketChangeAsync(
        TopFiveBasket newBasket,
        TopFiveBasket previousBasket,
        CancellationToken ct = default)
    {
        var removedAssetIds = previousBasket.Compositions
            .Select(c => c.AssetId)
            .Except(newBasket.Compositions.Select(c => c.AssetId))
            .ToHashSet();

        var customers = await _customerRepository.GetAllActiveAsync();

        foreach (var customer in customers)
        {
            var events = await _eventStore.GetEventsAsync(customer.Id, ct);
            var aggregate = CustomerCustodyAggregate.Recreate(events);
            var salesThisMonth = new List<AssetsSoldRebalancing>();

            foreach (var assetId in removedAssetIds)
            {
                var custodyItem = await _customerCustodyRepository.GetByCustomerAndAssetAsync(customer.Id, assetId);
                if (custodyItem is null || custodyItem.Quantity <= 0) continue;

                var price = await _assetPriceRepository.GetLastClosingPriceAsync(custodyItem.Asset.Ticker);
                if (price is null or 0) continue;

                decimal netProfit = (price.Value - custodyItem.AveragePrice) * custodyItem.Quantity;

                var saleEvent = new AssetsSoldRebalancing(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    customer.Id,
                    custodyItem.Asset.Ticker,
                    custodyItem.Quantity,
                    price.Value,
                    netProfit);

                aggregate.RegisterSale(custodyItem.Asset.Ticker, custodyItem.Quantity, price.Value, netProfit);
                salesThisMonth.Add(saleEvent);

                custodyItem.Quantity = 0;
                await _customerCustodyRepository.UpsertAsync(custodyItem);
            }

            await _eventStore.AppendAsync(aggregate.GetEvents(), ct);

            await PublishRebalancingIrIfNeeded(customer.Id, salesThisMonth, aggregate, ct);
        }
    }

    // 4.4B — Proportion deviation (no-op overload; BackgroundService uses the overload with basket)
    public Task RebalanceOnDeviationAsync(CancellationToken ct = default) =>
        Task.FromException(new InvalidOperationException("Use RebalanceOnDeviationAsync(basket, ct) overload."));

    public async Task RebalanceOnDeviationAsync(
        TopFiveBasket basket,
        CancellationToken ct = default)
    {
        var customers = await _customerRepository.GetAllActiveAsync();

        foreach (var customer in customers)
        {
            var custodyItems = await _customerCustodyRepository.GetByCustomerIdAsync(customer.Id);
            if (custodyItems.Count == 0) continue;

            // Calculate total portfolio value
            decimal totalValue = 0m;
            var prices = new Dictionary<Guid, decimal>();

            foreach (var item in custodyItems)
            {
                var price = await _assetPriceRepository.GetLastClosingPriceAsync(item.Asset.Ticker);
                if (price is null or 0) continue;
                prices[item.AssetId] = price.Value;
                totalValue += item.Quantity * price.Value;
            }

            if (totalValue == 0) continue;

            var events = await _eventStore.GetEventsAsync(customer.Id, ct);
            var aggregate = CustomerCustodyAggregate.Recreate(events);
            var salesThisMonth = new List<AssetsSoldRebalancing>();

            foreach (var composition in basket.Compositions)
            {
                var custodyItem = custodyItems.FirstOrDefault(i => i.AssetId == composition.AssetId);
                if (custodyItem is null || !prices.TryGetValue(composition.AssetId, out decimal price)) continue;

                decimal currentWeight = (custodyItem.Quantity * price) / totalValue * 100m;
                decimal targetWeight = composition.Percentage;
                decimal deviation = Math.Abs(currentWeight - targetWeight);

                if (deviation <= _deviationThreshold) continue;

                decimal targetValue = totalValue * targetWeight / 100m;
                decimal currentValue = custodyItem.Quantity * price;

                if (currentWeight > targetWeight)
                {
                    // Sell excess
                    decimal excessValue = currentValue - targetValue;
                    decimal qtyToSell = Math.Floor(excessValue / price);
                    if (qtyToSell <= 0) continue;

                    decimal netProfit = (price - custodyItem.AveragePrice) * qtyToSell;

                    var saleEvent = new AssetsSoldRebalancing(
                        Guid.NewGuid(), DateTime.UtcNow,
                        customer.Id, composition.Asset.Ticker,
                        qtyToSell, price, netProfit);

                    aggregate.RegisterSale(composition.Asset.Ticker, qtyToSell, price, netProfit);
                    salesThisMonth.Add(saleEvent);

                    custodyItem.Quantity -= qtyToSell;
                    await _customerCustodyRepository.UpsertAsync(custodyItem);
                }
                else
                {
                    // Buy shortfall
                    decimal shortfallValue = targetValue - currentValue;
                    decimal qtyToBuy = Math.Floor(shortfallValue / price);
                    if (qtyToBuy <= 0) continue;

                    decimal irTax = _irCalculationService.CalculateDedoDuro(qtyToBuy * price);
                    aggregate.RegisterContribuition(composition.Asset.Ticker, qtyToBuy, price, irTax, Guid.Empty);

                    decimal prevQty = custodyItem.Quantity;
                    decimal prevAvg = custodyItem.AveragePrice;
                    custodyItem.Quantity = prevQty + qtyToBuy;
                    if (custodyItem.Quantity > 0)
                        custodyItem.AveragePrice = (prevQty * prevAvg + qtyToBuy * price) / custodyItem.Quantity;

                    await _customerCustodyRepository.UpsertAsync(custodyItem);
                }
            }

            await _eventStore.AppendAsync(aggregate.GetEvents(), ct);
            await PublishRebalancingIrIfNeeded(customer.Id, salesThisMonth, aggregate, ct);
        }
    }

    private async Task PublishRebalancingIrIfNeeded(
        Guid customerId,
        List<AssetsSoldRebalancing> sales,
        CustomerCustodyAggregate aggregate,
        CancellationToken ct)
    {
        if (sales.Count == 0) return;

        decimal ir = _irCalculationService.CalculateRebalancingIr(
            sales,
            ticker => aggregate.AveragePrice.GetValueOrDefault(ticker, 0m));

        if (ir <= 0) return;

        decimal totalSales = sales.Sum(s => s.Quantity * s.UnityPrice);
        decimal netProfit = sales.Sum(s => s.NetProfit);

        await _kafkaProducer.PublishAsync(_irRebalancingTopic, new IrRebalancingEvent(
            customerId,
            DateTime.UtcNow.Month,
            DateTime.UtcNow.Year,
            totalSales,
            netProfit,
            ir), ct);
    }
}

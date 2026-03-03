using Application.Events;
using Application.Interfaces;
using Domain.Aggregates;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Services;

public class PurchaseEngineService : IPurchaseEngineService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITopFiveBasketRepository _basketRepository;
    private readonly IAssetPriceRepository _assetPriceRepository;
    private readonly IMasterCustodyRepository _masterCustodyRepository;
    private readonly ICustomerCustodyRepository _customerCustodyRepository;
    private readonly IBuyCycleRepository _buyCycleRepository;
    private readonly IEventStore _eventStore;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IrCalculationService _irCalculationService;
    private readonly string _irDedoDuroTopic;

    public PurchaseEngineService(
        ICustomerRepository customerRepository,
        ITopFiveBasketRepository basketRepository,
        IAssetPriceRepository assetPriceRepository,
        IMasterCustodyRepository masterCustodyRepository,
        ICustomerCustodyRepository customerCustodyRepository,
        IBuyCycleRepository buyCycleRepository,
        IEventStore eventStore,
        IKafkaProducer kafkaProducer,
        IrCalculationService irCalculationService,
        string irDedoDuroTopic)
    {
        _customerRepository = customerRepository;
        _basketRepository = basketRepository;
        _assetPriceRepository = assetPriceRepository;
        _masterCustodyRepository = masterCustodyRepository;
        _customerCustodyRepository = customerCustodyRepository;
        _buyCycleRepository = buyCycleRepository;
        _eventStore = eventStore;
        _kafkaProducer = kafkaProducer;
        _irCalculationService = irCalculationService;
        _irDedoDuroTopic = irDedoDuroTopic;
    }

    public async Task ExecuteAsync(DateTime date, Installment installment, CancellationToken ct = default)
    {
        if (await _buyCycleRepository.AlreadyExecutedAsync(date.Year, date.Month, installment))
            return;

        var cycle = new BuyCycle
        {
            Id = Guid.NewGuid(),
            Date = date,
            Installment = installment,
            Status = CycleStatus.Pending
        };

        var customers = await _customerRepository.GetAllActiveAsync();
        if (customers.Count == 0) return;

        var basket = await _basketRepository.GetActiveAsync();
        if (basket is null) return;

        decimal totalPurchase = customers.Sum(c => c.MonthlyContribution / 3m);
        cycle.TotalValue = totalPurchase;

        // Step 1: calculate how much to buy per asset (after deducting master balance)
        // Save previous master balances so step 2 can compute totalAvailable correctly (BUG 6 fix)
        var purchasedQuantities = new Dictionary<Guid, decimal>();
        var previousMasterBalances = new Dictionary<Guid, decimal>();

        foreach (var composition in basket.Compositions)
        {
            var price = await _assetPriceRepository.GetLastClosingPriceAsync(composition.Asset.Ticker);
            if (price is null or 0) continue;

            // BUG 5 fix: TRUNCAR(Valor / Cotacao) conforme RN-028
            decimal targetQty = Math.Floor((totalPurchase * composition.Percentage / 100m) / price.Value);

            var masterItem = await _masterCustodyRepository.GetByAssetIdAsync(composition.AssetId);
            decimal masterBalance = masterItem?.Quantity ?? 0m;
            previousMasterBalances[composition.AssetId] = masterBalance;

            decimal toBuy = Math.Max(0m, targetQty - masterBalance);

            // Split into standard lots (multiplos de 100) and fractional remainder
            decimal standardLots = Math.Floor(toBuy / 100m) * 100m;
            decimal fractional = toBuy - standardLots;
            decimal totalBought = standardLots + fractional;

            var masterCustodyItem = masterItem ?? new MasterCustodyItem
            {
                Id = Guid.NewGuid(),
                AssetId = composition.AssetId
            };

            decimal prevMasterQty = masterCustodyItem.Quantity;
            decimal prevMasterAvg = masterCustodyItem.AveragePrice;
            masterCustodyItem.Quantity = prevMasterQty + totalBought;
            if (masterCustodyItem.Quantity > 0)
                masterCustodyItem.AveragePrice = (prevMasterQty * prevMasterAvg + totalBought * price.Value)
                                                 / masterCustodyItem.Quantity;

            await _masterCustodyRepository.UpsertAsync(masterCustodyItem);
            purchasedQuantities[composition.AssetId] = totalBought;
        }

        // Step 2: distribute to each customer proportionally
        var distributedPerAsset = new Dictionary<Guid, decimal>();

        foreach (var composition in basket.Compositions)
        {
            if (!purchasedQuantities.TryGetValue(composition.AssetId, out decimal boughtQty)) continue;

            var price = await _assetPriceRepository.GetLastClosingPriceAsync(composition.Asset.Ticker);
            if (price is null or 0) continue;

            // BUG 6 fix: distribuir sobre total disponível (comprado + saldo master anterior) conforme RN-037
            decimal prevBalance = previousMasterBalances.GetValueOrDefault(composition.AssetId, 0m);
            decimal totalAvailable = boughtQty + prevBalance;

            decimal totalDistributed = 0m;

            foreach (var customer in customers)
            {
                decimal proportion = (customer.MonthlyContribution / 3m) / totalPurchase;
                decimal customerQty = Math.Floor(totalAvailable * proportion);

                if (customerQty <= 0) continue;

                var events = await _eventStore.GetEventsAsync(customer.Id, ct);
                var aggregate = CustomerCustodyAggregate.Recreate(events);

                decimal irTax = _irCalculationService.CalculateDedoDuro(customerQty * price.Value);

                aggregate.RegisterContribuition(
                    composition.Asset.Ticker,
                    customerQty,
                    price.Value,
                    irTax,
                    cycle.Id);

                await _eventStore.AppendAsync(aggregate.GetEvents(), ct);

                var custodyItem = await _customerCustodyRepository.GetByCustomerAndAssetAsync(customer.Id, composition.AssetId)
                    ?? new CustomerCustodyItem { Id = Guid.NewGuid(), CustomerId = customer.Id, AssetId = composition.AssetId };

                decimal prevQty = custodyItem.Quantity;
                decimal prevAvg = custodyItem.AveragePrice;
                custodyItem.Quantity = prevQty + customerQty;
                if (custodyItem.Quantity > 0)
                    custodyItem.AveragePrice = (prevQty * prevAvg + customerQty * price.Value) / custodyItem.Quantity;

                await _customerCustodyRepository.UpsertAsync(custodyItem);

                // Publish IR dedo-duro to Kafka (CPF incluído conforme RN-056)
                await _kafkaProducer.PublishAsync(_irDedoDuroTopic, new IrDedoDuroEvent(
                    customer.Id,
                    customer.CPF,
                    date,
                    composition.Asset.Ticker,
                    customerQty,
                    price.Value,
                    customerQty * price.Value,
                    irTax,
                    cycle.Id), ct);

                totalDistributed += customerQty;
            }

            distributedPerAsset[composition.AssetId] = totalDistributed;

            // BUG 7 fix: sempre atualizar master com o residual, mesmo quando residual = 0
            decimal residual = totalAvailable - totalDistributed;
            var masterItemFinal = await _masterCustodyRepository.GetByAssetIdAsync(composition.AssetId);
            if (masterItemFinal is not null)
            {
                masterItemFinal.Quantity = residual;
                await _masterCustodyRepository.UpsertAsync(masterItemFinal);
            }
        }

        cycle.Status = CycleStatus.Executed;
        cycle.ExecutedAt = DateTime.UtcNow;
        await _buyCycleRepository.AddAsync(cycle);
    }
}

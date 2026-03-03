using Application.Events;
using Application.Interfaces;
using Application.Services;
using Domain.Aggregates;
using Domain.Entities;
using Domain.Enums;
using Domain.Events.Base;
using Domain.Interfaces;
using Moq;
using Xunit;

namespace UnitTests.Services;

public class RebalancingEngineServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IAssetPriceRepository> _priceRepo = new();
    private readonly Mock<ICustomerCustodyRepository> _custodyRepo = new();
    private readonly Mock<IEventStore> _eventStore = new();
    private readonly Mock<IKafkaProducer> _kafkaProducer = new();
    private readonly Mock<IIrCalculationService> _irService = new();
    private readonly RebalancingEngineService _sut;

    public RebalancingEngineServiceTests()
    {
        _sut = new RebalancingEngineService(
            _customerRepo.Object,
            _priceRepo.Object,
            _custodyRepo.Object,
            _eventStore.Object,
            _kafkaProducer.Object,
            _irService.Object,
            deviationThreshold: 5m,
            irRebalancingTopic: "ir-rebalancing");

        _eventStore.Setup(r => r.GetEventsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<DomainEvent>());
        _eventStore.Setup(r => r.AppendAsync(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _irService.Setup(r => r.CalculateDedoDuro(It.IsAny<decimal>())).Returns(0m);
        _irService.Setup(r => r.CalculateRebalancingIr(It.IsAny<IEnumerable<Domain.Events.AssetsSoldRebalancing>>(), It.IsAny<Func<string, decimal>>()))
            .Returns(0m);
    }

    // =========================================================
    // 4.4A — RebalanceOnBasketChangeAsync
    // =========================================================

    [Fact]
    public async Task RebalanceOnBasketChange_NoRemovedAssets_DoesNotSellAnything()
    {
        var assetId = Guid.NewGuid();
        var asset = new Asset { Id = assetId, Ticker = "PETR4", Name = "Petrobras" };
        var composition = new BasketComposition { AssetId = assetId, Asset = asset, Percentage = 100m };

        var previousBasket = new TopFiveBasket { Id = Guid.NewGuid(), Compositions = new List<BasketComposition> { composition } };
        var newBasket = new TopFiveBasket { Id = Guid.NewGuid(), Compositions = new List<BasketComposition> { composition } };

        var customer = new Customer { Id = Guid.NewGuid(), MonthlyContribution = 300m };
        _customerRepo.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<Customer> { customer });

        await _sut.RebalanceOnBasketChangeAsync(newBasket, previousBasket);

        _custodyRepo.Verify(r => r.UpsertAsync(It.IsAny<CustomerCustodyItem>()), Times.Never);
    }

    [Fact]
    public async Task RebalanceOnBasketChange_RemovedAsset_ZeroesCustomerPosition()
    {
        // VALE3 foi removida da cesta
        var petr4Id = Guid.NewGuid();
        var vale3Id = Guid.NewGuid();
        var petr4 = new Asset { Id = petr4Id, Ticker = "PETR4", Name = "Petrobras" };
        var vale3 = new Asset { Id = vale3Id, Ticker = "VALE3", Name = "Vale" };

        var previousBasket = new TopFiveBasket
        {
            Id = Guid.NewGuid(),
            Compositions = new List<BasketComposition>
            {
                new() { AssetId = petr4Id, Asset = petr4, Percentage = 50m },
                new() { AssetId = vale3Id, Asset = vale3, Percentage = 50m }
            }
        };
        var newBasket = new TopFiveBasket
        {
            Id = Guid.NewGuid(),
            Compositions = new List<BasketComposition>
            {
                new() { AssetId = petr4Id, Asset = petr4, Percentage = 100m }
            }
        };

        var customer = new Customer { Id = Guid.NewGuid(), MonthlyContribution = 300m };
        var vale3Custody = new CustomerCustodyItem
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            AssetId = vale3Id,
            Asset = vale3,
            Quantity = 10m,
            AveragePrice = 60m
        };

        _customerRepo.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<Customer> { customer });
        _custodyRepo.Setup(r => r.GetByCustomerAndAssetAsync(customer.Id, vale3Id)).ReturnsAsync(vale3Custody);
        _custodyRepo.Setup(r => r.UpsertAsync(It.IsAny<CustomerCustodyItem>())).Returns(Task.CompletedTask);
        _priceRepo.Setup(r => r.GetLastClosingPriceAsync("VALE3")).ReturnsAsync(65m);

        await _sut.RebalanceOnBasketChangeAsync(newBasket, previousBasket);

        // Position deve ser zerada
        _custodyRepo.Verify(r => r.UpsertAsync(
            It.Is<CustomerCustodyItem>(i => i.AssetId == vale3Id && i.Quantity == 0m)), Times.Once);
    }

    [Fact]
    public async Task RebalanceOnBasketChange_CustomerHasNoPositionInRemovedAsset_Skips()
    {
        var petr4Id = Guid.NewGuid();
        var vale3Id = Guid.NewGuid();
        var petr4 = new Asset { Id = petr4Id, Ticker = "PETR4", Name = "Petrobras" };
        var vale3 = new Asset { Id = vale3Id, Ticker = "VALE3", Name = "Vale" };

        var previousBasket = new TopFiveBasket
        {
            Id = Guid.NewGuid(),
            Compositions = new List<BasketComposition>
            {
                new() { AssetId = petr4Id, Asset = petr4, Percentage = 50m },
                new() { AssetId = vale3Id, Asset = vale3, Percentage = 50m }
            }
        };
        var newBasket = new TopFiveBasket
        {
            Id = Guid.NewGuid(),
            Compositions = new List<BasketComposition>
            {
                new() { AssetId = petr4Id, Asset = petr4, Percentage = 100m }
            }
        };

        var customer = new Customer { Id = Guid.NewGuid() };
        _customerRepo.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<Customer> { customer });
        // Cliente não tem custódia de VALE3
        _custodyRepo.Setup(r => r.GetByCustomerAndAssetAsync(customer.Id, vale3Id))
            .ReturnsAsync((CustomerCustodyItem?)null);

        await _sut.RebalanceOnBasketChangeAsync(newBasket, previousBasket);

        _custodyRepo.Verify(r => r.UpsertAsync(It.IsAny<CustomerCustodyItem>()), Times.Never);
    }

    // =========================================================
    // 4.4B — RebalanceOnDeviationAsync
    // =========================================================

    [Fact]
    public async Task RebalanceOnDeviation_AssetWithinThreshold_DoesNothing()
    {
        // PETR4: 100% alvo, 100% atual (desvio 0% < threshold 5%) → sem ação
        var assetId = Guid.NewGuid();
        var asset = new Asset { Id = assetId, Ticker = "PETR4", Name = "Petrobras" };
        var basket = BuildBasket(new[] { (assetId, asset, 100m) });

        var customer = new Customer { Id = Guid.NewGuid() };
        var custodyItem = new CustomerCustodyItem
        {
            Id = Guid.NewGuid(), CustomerId = customer.Id, AssetId = assetId,
            Asset = asset, Quantity = 100m, AveragePrice = 35m
        };

        _customerRepo.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<Customer> { customer });
        _custodyRepo.Setup(r => r.GetByCustomerIdAsync(customer.Id))
            .ReturnsAsync(new List<CustomerCustodyItem> { custodyItem });
        _priceRepo.Setup(r => r.GetLastClosingPriceAsync("PETR4")).ReturnsAsync(35m);

        await _sut.RebalanceOnDeviationAsync(basket);

        _custodyRepo.Verify(r => r.UpsertAsync(It.IsAny<CustomerCustodyItem>()), Times.Never);
    }

    [Fact]
    public async Task RebalanceOnDeviation_OverweightAsset_SellsExcess()
    {
        // PETR4: 50% alvo, 85% atual (desvio 35% > threshold 5%) → vende excesso
        // Portfolio: 100 PETR4 a R$35 = R$3500
        // VALE3 a R$62: 10 ações = R$620
        // Total = 4120
        // PETR4 peso atual = 3500/4120 = 84.95% > 50% → vende excesso
        // Excesso = 3500 - 4120*50% = 3500 - 2060 = 1440
        // qty a vender = floor(1440 / 35) = 41
        var petr4Id = Guid.NewGuid();
        var vale3Id = Guid.NewGuid();
        var petr4 = new Asset { Id = petr4Id, Ticker = "PETR4", Name = "Petrobras" };
        var vale3 = new Asset { Id = vale3Id, Ticker = "VALE3", Name = "Vale" };
        var basket = BuildBasket(new[] { (petr4Id, petr4, 50m), (vale3Id, vale3, 50m) });

        var customer = new Customer { Id = Guid.NewGuid() };
        var petr4Custody = new CustomerCustodyItem
        {
            Id = Guid.NewGuid(), CustomerId = customer.Id, AssetId = petr4Id,
            Asset = petr4, Quantity = 100m, AveragePrice = 30m
        };
        var vale3Custody = new CustomerCustodyItem
        {
            Id = Guid.NewGuid(), CustomerId = customer.Id, AssetId = vale3Id,
            Asset = vale3, Quantity = 10m, AveragePrice = 60m
        };

        _customerRepo.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<Customer> { customer });
        _custodyRepo.Setup(r => r.GetByCustomerIdAsync(customer.Id))
            .ReturnsAsync(new List<CustomerCustodyItem> { petr4Custody, vale3Custody });
        _custodyRepo.Setup(r => r.UpsertAsync(It.IsAny<CustomerCustodyItem>())).Returns(Task.CompletedTask);
        _priceRepo.Setup(r => r.GetLastClosingPriceAsync("PETR4")).ReturnsAsync(35m);
        _priceRepo.Setup(r => r.GetLastClosingPriceAsync("VALE3")).ReturnsAsync(62m);

        await _sut.RebalanceOnDeviationAsync(basket);

        // PETR4 overweight: deve vender 41 ações
        // Total = 4120; target PETR4 = 2060; excess = 3500-2060=1440; qty=floor(1440/35)=41
        _custodyRepo.Verify(r => r.UpsertAsync(
            It.Is<CustomerCustodyItem>(i => i.AssetId == petr4Id && i.Quantity == 59m)), Times.Once);
    }

    [Fact]
    public async Task RebalanceOnDeviation_NoCustomers_DoesNothing()
    {
        _customerRepo.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<Customer>());
        var basket = BuildBasket(Array.Empty<(Guid, Asset, decimal)>());

        await _sut.RebalanceOnDeviationAsync(basket);

        _custodyRepo.Verify(r => r.GetByCustomerIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    // --- Helpers ---

    private static TopFiveBasket BuildBasket(IEnumerable<(Guid assetId, Asset asset, decimal percentage)> items) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Top Five",
            Status = BasketStatus.Active,
            Compositions = items.Select(i => new BasketComposition
            {
                Id = Guid.NewGuid(),
                AssetId = i.assetId,
                Asset = i.asset,
                Percentage = i.percentage
            }).ToList()
        };
}

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

public class PurchaseEngineServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<ITopFiveBasketRepository> _basketRepo = new();
    private readonly Mock<IAssetPriceRepository> _priceRepo = new();
    private readonly Mock<IMasterCustodyRepository> _masterCustodyRepo = new();
    private readonly Mock<ICustomerCustodyRepository> _customerCustodyRepo = new();
    private readonly Mock<IBuyCycleRepository> _buyCycleRepo = new();
    private readonly Mock<IEventStore> _eventStore = new();
    private readonly Mock<IKafkaProducer> _kafkaProducer = new();
    private readonly Mock<IIrCalculationService> _irService = new();
    private readonly PurchaseEngineService _sut;

    public PurchaseEngineServiceTests()
    {
        _sut = new PurchaseEngineService(
            _customerRepo.Object,
            _basketRepo.Object,
            _priceRepo.Object,
            _masterCustodyRepo.Object,
            _customerCustodyRepo.Object,
            _buyCycleRepo.Object,
            _eventStore.Object,
            _kafkaProducer.Object,
            _irService.Object,
            "ir-dedo-duro");
    }

    // --- Idempotência ---

    [Fact]
    public async Task ExecuteAsync_AlreadyExecuted_DoesNothing()
    {
        _buyCycleRepo.Setup(r => r.AlreadyExecutedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Installment>()))
            .ReturnsAsync(true);

        await _sut.ExecuteAsync(new DateTime(2026, 3, 5), Installment.Day5);

        _customerRepo.Verify(r => r.GetAllActiveAsync(), Times.Never);
    }

    // --- Guards de entrada ---

    [Fact]
    public async Task ExecuteAsync_NoActiveCustomers_DoesNothing()
    {
        _buyCycleRepo.Setup(r => r.AlreadyExecutedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Installment>()))
            .ReturnsAsync(false);
        _customerRepo.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<Customer>());

        await _sut.ExecuteAsync(new DateTime(2026, 3, 5), Installment.Day5);

        _basketRepo.Verify(r => r.GetActiveAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NoActiveBasket_DoesNothing()
    {
        _buyCycleRepo.Setup(r => r.AlreadyExecutedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Installment>()))
            .ReturnsAsync(false);
        _customerRepo.Setup(r => r.GetAllActiveAsync())
            .ReturnsAsync(new List<Customer> { new() { Id = Guid.NewGuid(), MonthlyContribution = 300m } });
        _basketRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync((TopFiveBasket?)null);

        await _sut.ExecuteAsync(new DateTime(2026, 3, 5), Installment.Day5);

        _priceRepo.Verify(r => r.GetLastClosingPriceAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NullPrice_SkipsAsset()
    {
        var assetId = Guid.NewGuid();
        var asset = new Asset { Id = assetId, Ticker = "PETR4", Name = "Petrobras" };
        var customer = new Customer { Id = Guid.NewGuid(), MonthlyContribution = 300m };
        var basket = BuildBasket(assetId, asset, 100m);

        SetupBaselineStubs(alreadyExecuted: false);
        _customerRepo.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<Customer> { customer });
        _basketRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(basket);
        _priceRepo.Setup(r => r.GetLastClosingPriceAsync("PETR4")).ReturnsAsync((decimal?)null);

        await _sut.ExecuteAsync(new DateTime(2026, 3, 5), Installment.Day5);

        _customerCustodyRepo.Verify(r => r.UpsertAsync(It.IsAny<CustomerCustodyItem>()), Times.Never);
    }

    // --- Distribuição proporcional ---

    [Fact]
    public async Task ExecuteAsync_TwoCustomers_DistributesProportionallyWithTruncation()
    {
        // Arrange
        // Clientes A (R$300/mês) e B (R$600/mês)
        // Parcela = 1/3: A = 100, B = 200, total = 300
        // PETR4 a R$35: target = floor(300 / 35) = 8
        // Prop A = 100/300: floor(8 * 0.333) = 2
        // Prop B = 200/300: floor(8 * 0.666) = 5
        // Total distribuído = 7, residual master = 1
        var (assetId, asset, customerA, customerB, basket) = BuildScenario();

        SetupBaselineStubs(alreadyExecuted: false);
        _customerRepo.Setup(r => r.GetAllActiveAsync())
            .ReturnsAsync(new List<Customer> { customerA, customerB });
        _basketRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(basket);
        _priceRepo.Setup(r => r.GetLastClosingPriceAsync("PETR4")).ReturnsAsync(35m);
        _masterCustodyRepo.Setup(r => r.GetByAssetIdAsync(assetId)).ReturnsAsync((MasterCustodyItem?)null);
        _masterCustodyRepo.Setup(r => r.UpsertAsync(It.IsAny<MasterCustodyItem>())).Returns(Task.CompletedTask);
        _customerCustodyRepo.Setup(r => r.GetByCustomerAndAssetAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((CustomerCustodyItem?)null);
        _customerCustodyRepo.Setup(r => r.UpsertAsync(It.IsAny<CustomerCustodyItem>())).Returns(Task.CompletedTask);
        _eventStore.Setup(r => r.GetEventsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<DomainEvent>());
        _eventStore.Setup(r => r.AppendAsync(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _irService.Setup(r => r.CalculateDedoDuro(It.IsAny<decimal>())).Returns(0.01m);
        _kafkaProducer.Setup(r => r.PublishAsync(It.IsAny<string>(), It.IsAny<IrDedoDuroEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecuteAsync(new DateTime(2026, 3, 5), Installment.Day5);

        // Assert: customer A recebe 2, B recebe 5
        _customerCustodyRepo.Verify(r => r.UpsertAsync(
            It.Is<CustomerCustodyItem>(i => i.CustomerId == customerA.Id && i.Quantity == 2m)), Times.Once);
        _customerCustodyRepo.Verify(r => r.UpsertAsync(
            It.Is<CustomerCustodyItem>(i => i.CustomerId == customerB.Id && i.Quantity == 5m)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AfterDistribution_SavesResidualInMaster()
    {
        // Same scenario: total = 8, distributed = 7, residual = 1
        var (assetId, asset, customerA, customerB, basket) = BuildScenario();

        SetupBaselineStubs(alreadyExecuted: false);
        _customerRepo.Setup(r => r.GetAllActiveAsync())
            .ReturnsAsync(new List<Customer> { customerA, customerB });
        _basketRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(basket);
        _priceRepo.Setup(r => r.GetLastClosingPriceAsync("PETR4")).ReturnsAsync(35m);

        // First call (step 1): returns null (new item)
        // Subsequent calls (step 2 residual query): return the item saved in step 1
        var savedMasterItem = new MasterCustodyItem { Id = Guid.NewGuid(), AssetId = assetId, Quantity = 8m };
        _masterCustodyRepo.SetupSequence(r => r.GetByAssetIdAsync(assetId))
            .ReturnsAsync((MasterCustodyItem?)null) // step 1: no existing item
            .ReturnsAsync(savedMasterItem);          // step 2: residual update
        _masterCustodyRepo.Setup(r => r.UpsertAsync(It.IsAny<MasterCustodyItem>())).Returns(Task.CompletedTask);
        _customerCustodyRepo.Setup(r => r.GetByCustomerAndAssetAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((CustomerCustodyItem?)null);
        _customerCustodyRepo.Setup(r => r.UpsertAsync(It.IsAny<CustomerCustodyItem>())).Returns(Task.CompletedTask);
        _eventStore.Setup(r => r.GetEventsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<DomainEvent>());
        _eventStore.Setup(r => r.AppendAsync(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _irService.Setup(r => r.CalculateDedoDuro(It.IsAny<decimal>())).Returns(0m);
        _kafkaProducer.Setup(r => r.PublishAsync(It.IsAny<string>(), It.IsAny<IrDedoDuroEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ExecuteAsync(new DateTime(2026, 3, 5), Installment.Day5);

        // Residual = 8 - 7 = 1 deve ser salvo na master
        _masterCustodyRepo.Verify(r => r.UpsertAsync(
            It.Is<MasterCustodyItem>(m => m.Quantity == 1m)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Completes_SavesCycleAsExecuted()
    {
        var (assetId, asset, customerA, customerB, basket) = BuildScenario();

        SetupBaselineStubs(alreadyExecuted: false);
        _customerRepo.Setup(r => r.GetAllActiveAsync())
            .ReturnsAsync(new List<Customer> { customerA, customerB });
        _basketRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(basket);
        _priceRepo.Setup(r => r.GetLastClosingPriceAsync("PETR4")).ReturnsAsync(35m);
        _masterCustodyRepo.Setup(r => r.GetByAssetIdAsync(assetId)).ReturnsAsync((MasterCustodyItem?)null);
        _masterCustodyRepo.Setup(r => r.UpsertAsync(It.IsAny<MasterCustodyItem>())).Returns(Task.CompletedTask);
        _customerCustodyRepo.Setup(r => r.GetByCustomerAndAssetAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((CustomerCustodyItem?)null);
        _customerCustodyRepo.Setup(r => r.UpsertAsync(It.IsAny<CustomerCustodyItem>())).Returns(Task.CompletedTask);
        _eventStore.Setup(r => r.GetEventsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<DomainEvent>());
        _eventStore.Setup(r => r.AppendAsync(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _irService.Setup(r => r.CalculateDedoDuro(It.IsAny<decimal>())).Returns(0m);
        _kafkaProducer.Setup(r => r.PublishAsync(It.IsAny<string>(), It.IsAny<IrDedoDuroEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ExecuteAsync(new DateTime(2026, 3, 5), Installment.Day5);

        _buyCycleRepo.Verify(r => r.AddAsync(
            It.Is<BuyCycle>(c => c.Status == CycleStatus.Executed)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_PublishesIrDedoDuroPerCustomerPerAsset()
    {
        var (assetId, asset, customerA, customerB, basket) = BuildScenario();

        SetupBaselineStubs(alreadyExecuted: false);
        _customerRepo.Setup(r => r.GetAllActiveAsync())
            .ReturnsAsync(new List<Customer> { customerA, customerB });
        _basketRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(basket);
        _priceRepo.Setup(r => r.GetLastClosingPriceAsync("PETR4")).ReturnsAsync(35m);
        _masterCustodyRepo.Setup(r => r.GetByAssetIdAsync(assetId)).ReturnsAsync((MasterCustodyItem?)null);
        _masterCustodyRepo.Setup(r => r.UpsertAsync(It.IsAny<MasterCustodyItem>())).Returns(Task.CompletedTask);
        _customerCustodyRepo.Setup(r => r.GetByCustomerAndAssetAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((CustomerCustodyItem?)null);
        _customerCustodyRepo.Setup(r => r.UpsertAsync(It.IsAny<CustomerCustodyItem>())).Returns(Task.CompletedTask);
        _eventStore.Setup(r => r.GetEventsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<DomainEvent>());
        _eventStore.Setup(r => r.AppendAsync(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _irService.Setup(r => r.CalculateDedoDuro(It.IsAny<decimal>())).Returns(0.01m);
        _kafkaProducer.Setup(r => r.PublishAsync(It.IsAny<string>(), It.IsAny<IrDedoDuroEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ExecuteAsync(new DateTime(2026, 3, 5), Installment.Day5);

        // 2 clientes × 1 ativo = 2 publicações de IR dedo-duro
        _kafkaProducer.Verify(r => r.PublishAsync(
            "ir-dedo-duro",
            It.IsAny<IrDedoDuroEvent>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // --- Helpers ---

    private void SetupBaselineStubs(bool alreadyExecuted)
    {
        _buyCycleRepo.Setup(r => r.AlreadyExecutedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Installment>()))
            .ReturnsAsync(alreadyExecuted);
        _buyCycleRepo.Setup(r => r.AddAsync(It.IsAny<BuyCycle>())).Returns(Task.CompletedTask);
    }

    private static (Guid assetId, Asset asset, Customer customerA, Customer customerB, TopFiveBasket basket) BuildScenario()
    {
        var assetId = Guid.NewGuid();
        var asset = new Asset { Id = assetId, Ticker = "PETR4", Name = "Petrobras" };

        var customerA = new Customer
        {
            Id = Guid.NewGuid(),
            MonthlyContribution = 300m,
            Name = "Cliente A",
            CPF = "11111111111",
            Email = "a@test.com",
            Status = CustomerStatus.Active
        };
        var customerB = new Customer
        {
            Id = Guid.NewGuid(),
            MonthlyContribution = 600m,
            Name = "Cliente B",
            CPF = "22222222222",
            Email = "b@test.com",
            Status = CustomerStatus.Active
        };

        return (assetId, asset, customerA, customerB, BuildBasket(assetId, asset, 100m));
    }

    private static TopFiveBasket BuildBasket(Guid assetId, Asset asset, decimal percentage) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Top Five",
            Status = BasketStatus.Active,
            Compositions = new List<BasketComposition>
            {
                new() { Id = Guid.NewGuid(), AssetId = assetId, Asset = asset, Percentage = percentage }
            }
        };
}

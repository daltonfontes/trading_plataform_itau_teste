using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Moq;
using Xunit;

namespace UnitTests.Services;

public class BasketServiceTests
{
    private readonly Mock<ITopFiveBasketRepository> _basketRepo = new();
    private readonly Mock<IAssetRepository> _assetRepo = new();
    private readonly Mock<IRebalancingEngineService> _rebalancingEngine = new();
    private readonly BasketService _sut;

    public BasketServiceTests()
    {
        _sut = new BasketService(
            _basketRepo.Object,
            _assetRepo.Object,
            _rebalancingEngine.Object);
    }

    private static List<(string Ticker, decimal Percentual)> ValidItens() =>
    [
        ("PETR4", 30m),
        ("VALE3", 25m),
        ("ITUB4", 20m),
        ("BBDC4", 15m),
        ("WEGE3", 10m)
    ];

    private void SetupAssetGetOrCreate()
    {
        _assetRepo.Setup(r => r.GetOrCreateAsync(It.IsAny<string>()))
            .ReturnsAsync((string t) => new Asset { Id = Guid.NewGuid(), Ticker = t, Name = t });
    }

    [Fact]
    public async Task CreateOrUpdateAsync_FirstBasket_CreatesWithoutRebalancing()
    {
        _basketRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync((TopFiveBasket?)null);
        _basketRepo.Setup(r => r.AddAsync(It.IsAny<TopFiveBasket>())).Returns(Task.CompletedTask);
        SetupAssetGetOrCreate();

        var result = await _sut.CreateOrUpdateAsync("Top Five Jan", ValidItens());

        Assert.True(result.Ativa);
        Assert.False(result.RebalanceamentoDisparado);
        Assert.Equal(5, result.Itens.Count);
        _rebalancingEngine.Verify(r => r.RebalanceOnBasketChangeAsync(
            It.IsAny<TopFiveBasket>(), It.IsAny<TopFiveBasket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_ExistingBasket_TriggersRebalancing()
    {
        var previous = new TopFiveBasket { Id = Guid.NewGuid(), Status = BasketStatus.Active, Compositions = [] };
        _basketRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(previous);
        _basketRepo.Setup(r => r.UpdateAsync(It.IsAny<TopFiveBasket>())).Returns(Task.CompletedTask);
        _basketRepo.Setup(r => r.AddAsync(It.IsAny<TopFiveBasket>())).Returns(Task.CompletedTask);
        _rebalancingEngine.Setup(r => r.RebalanceOnBasketChangeAsync(
            It.IsAny<TopFiveBasket>(), It.IsAny<TopFiveBasket>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        SetupAssetGetOrCreate();

        var result = await _sut.CreateOrUpdateAsync("Top Five Fev", ValidItens());

        Assert.True(result.RebalanceamentoDisparado);
        _basketRepo.Verify(r => r.UpdateAsync(It.Is<TopFiveBasket>(b => b.Status == BasketStatus.Inactive)), Times.Once);
        _rebalancingEngine.Verify(r => r.RebalanceOnBasketChangeAsync(
            It.IsAny<TopFiveBasket>(), It.IsAny<TopFiveBasket>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_LessThan5Assets_ThrowsException()
    {
        var itens = ValidItens().Take(4).ToList();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateOrUpdateAsync("Cesta Inválida", itens));

        Assert.Contains("QUANTIDADE_ATIVOS_INVALIDA", ex.Message);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_PercentualsDontSum100_ThrowsException()
    {
        var itens = new List<(string, decimal)>
        {
            ("PETR4", 30m), ("VALE3", 25m), ("ITUB4", 20m), ("BBDC4", 15m), ("WEGE3", 5m) // soma = 95%
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateOrUpdateAsync("Inválida", itens));

        Assert.Contains("PERCENTUAIS_INVALIDOS", ex.Message);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_ZeroPercentual_ThrowsException()
    {
        var itens = new List<(string, decimal)>
        {
            ("PETR4", 40m), ("VALE3", 30m), ("ITUB4", 20m), ("BBDC4", 10m), ("WEGE3", 0m)
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateOrUpdateAsync("Inválida", itens));

        Assert.Contains("PERCENTUAIS_INVALIDOS", ex.Message);
    }

    [Fact]
    public async Task GetActiveAsync_NoActiveBasket_ReturnsNull()
    {
        _basketRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync((TopFiveBasket?)null);

        var result = await _sut.GetActiveAsync();

        Assert.Null(result);
    }
}

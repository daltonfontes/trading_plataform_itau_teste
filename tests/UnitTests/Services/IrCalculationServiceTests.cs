using Application.Services;
using Domain.Events;
using Xunit;

namespace UnitTests.Services;

public class IrCalculationServiceTests
{
    private readonly IrCalculationService _sut = new();

    // --- IR Dedo-Duro ---

    [Fact]
    public void CalculateDedoDuro_ShouldReturn0005Percent()
    {
        decimal result = _sut.CalculateDedoDuro(280m);
        Assert.Equal(0.014m, result);
    }

    [Fact]
    public void CalculateDedoDuro_ZeroValue_ReturnsZero()
    {
        decimal result = _sut.CalculateDedoDuro(0m);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateDedoDuro_LargeValue_CorrectResult()
    {
        decimal result = _sut.CalculateDedoDuro(100_000m);
        Assert.Equal(5m, result); // 100000 * 0.005% = 5
    }

    // --- IR Rebalanceamento ---

    [Fact]
    public void CalculateRebalancingIr_TotalSalesBelow20k_ReturnsZero()
    {
        var sales = new[]
        {
            new AssetsSoldRebalancing(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), "BBDC4", 10, 15m, 10m),
            new AssetsSoldRebalancing(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), "WEGE3", 2, 40m, 4m)
        };
        // Total: 150 + 80 = 230, abaixo de 20k

        decimal result = _sut.CalculateRebalancingIr(sales, ticker => ticker == "BBDC4" ? 14m : 38m);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateRebalancingIr_TotalSalesAbove20k_Returns20PercentOnProfit()
    {
        var customerId = Guid.NewGuid();
        var sales = new[]
        {
            new AssetsSoldRebalancing(Guid.NewGuid(), DateTime.UtcNow, customerId, "BBDC4", 500, 16m, 1000m),
            new AssetsSoldRebalancing(Guid.NewGuid(), DateTime.UtcNow, customerId, "WEGE3", 300, 45m, 2100m)
        };
        // Total: 8000 + 13500 = 21500 > 20k
        // Lucro BBDC4: 500*(16-14)=1000, WEGE3: 300*(45-38)=2100 => total 3100
        // IR: 3100*20% = 620

        decimal result = _sut.CalculateRebalancingIr(sales, ticker => ticker == "BBDC4" ? 14m : 38m);

        Assert.Equal(620m, result);
    }

    [Fact]
    public void CalculateRebalancingIr_SalesAbove20kButNetLoss_ReturnsZero()
    {
        var customerId = Guid.NewGuid();
        var sales = new[]
        {
            new AssetsSoldRebalancing(Guid.NewGuid(), DateTime.UtcNow, customerId, "PETR4", 400, 32m, -1200m),
            new AssetsSoldRebalancing(Guid.NewGuid(), DateTime.UtcNow, customerId, "VALE3", 200, 58m, 600m)
        };
        // Total: 12800+11600 = 24400 > 20k
        // Lucro líquido: -1200+600 = -600 (prejuízo)

        decimal result = _sut.CalculateRebalancingIr(sales, ticker => ticker == "PETR4" ? 35m : 55m);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateRebalancingIr_ExactlyAt20k_ReturnsZero()
    {
        var sales = new[]
        {
            new AssetsSoldRebalancing(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), "PETR4", 100, 200m, 1000m)
        };
        // Total: 20000 = limite (não excede)

        decimal result = _sut.CalculateRebalancingIr(sales, _ => 190m);

        Assert.Equal(0m, result);
    }
}

using Domain.Events;

namespace Application.Services;

public class IrCalculationService
{
    private const decimal DedoDuroRate = 0.00005m; // 0.005%
    private const decimal RebalancingIrRate = 0.20m; // 20%
    private const decimal MonthlyExemptionLimit = 20_000m;

    public decimal CalculateDedoDuro(decimal totalValue) =>
        totalValue * DedoDuroRate;

    public decimal CalculateRebalancingIr(
        IEnumerable<AssetsSoldRebalancing> monthlySales,
        Func<string, decimal> getAveragePriceForTicker)
    {
        decimal totalSalesValue = monthlySales.Sum(s => s.Quantity * s.UnityPrice);

        if (totalSalesValue <= MonthlyExemptionLimit)
            return 0m;

        decimal netProfit = monthlySales.Sum(s =>
        {
            decimal averagePrice = getAveragePriceForTicker(s.Ticker);
            decimal profit = (s.UnityPrice - averagePrice) * s.Quantity;
            return profit;
        });

        return netProfit > 0 ? netProfit * RebalancingIrRate : 0m;
    }
}

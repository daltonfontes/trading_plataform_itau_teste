using Domain.Events;

namespace Application.Interfaces;

public interface IIrCalculationService
{
    decimal CalculateDedoDuro(decimal totalValue);
    decimal CalculateRebalancingIr(
        IEnumerable<AssetsSoldRebalancing> monthlySales,
        Func<string, decimal> getAveragePriceForTicker);
}

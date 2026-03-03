using Domain.Entities;

namespace Domain.Interfaces;

public interface IAssetPriceRepository
{
    Task<decimal?> GetLastClosingPriceAsync(string ticker);
    Task<AssetPrice?> GetLastByTickerAsync(string ticker);
    Task UpsertAsync(AssetPrice price);
}

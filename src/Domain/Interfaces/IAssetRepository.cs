using Domain.Entities;

namespace Domain.Interfaces;

public interface IAssetRepository
{
    Task<Asset?> GetByTickerAsync(string ticker);
    Task<Asset> GetOrCreateAsync(string ticker);
}

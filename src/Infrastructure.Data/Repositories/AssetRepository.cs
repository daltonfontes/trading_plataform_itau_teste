using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly DataContext _context;

    public AssetRepository(DataContext context) => _context = context;

    public async Task<Asset?> GetByTickerAsync(string ticker) =>
        await _context.Assets.FirstOrDefaultAsync(a => a.Ticker == ticker);

    public async Task<Asset> GetOrCreateAsync(string ticker)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Ticker == ticker);
        if (asset is not null) return asset;

        asset = new Asset { Id = Guid.NewGuid(), Ticker = ticker, Name = ticker };
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();
        return asset;
    }
}

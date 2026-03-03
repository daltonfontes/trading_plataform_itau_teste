using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class AssetPriceRepository : IAssetPriceRepository
{
    private readonly DataContext _context;

    public AssetPriceRepository(DataContext context) => _context = context;

    public async Task<decimal?> GetLastClosingPriceAsync(string ticker) =>
        (await GetLastByTickerAsync(ticker))?.ClosingPrice;

    public async Task<AssetPrice?> GetLastByTickerAsync(string ticker) =>
        await _context.AssetPrices
            .Include(p => p.Asset)
            .Where(p => p.Asset.Ticker == ticker)
            .OrderByDescending(p => p.TradingDate)
            .FirstOrDefaultAsync();

    public async Task UpsertAsync(AssetPrice price)
    {
        var existing = await _context.AssetPrices
            .FirstOrDefaultAsync(p => p.AssetId == price.AssetId && p.TradingDate == price.TradingDate);

        if (existing is null)
            _context.AssetPrices.Add(price);
        else
        {
            existing.ClosingPrice = price.ClosingPrice;
            _context.AssetPrices.Update(existing);
        }

        await _context.SaveChangesAsync();
    }
}

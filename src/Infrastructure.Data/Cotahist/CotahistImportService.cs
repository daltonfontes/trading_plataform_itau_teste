using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Cotahist;

public class CotahistImportService
{
    private readonly CotahistParser _parser;
    private readonly IAssetPriceRepository _assetPriceRepository;
    private readonly DataContext _context;

    public CotahistImportService(
        CotahistParser parser,
        IAssetPriceRepository assetPriceRepository,
        DataContext context)
    {
        _parser = parser;
        _assetPriceRepository = assetPriceRepository;
        _context = context;
    }

    public async Task ImportAsync(string filePath, CancellationToken ct = default)
    {
        var records = _parser.Parse(filePath);

        // Build ticker → asset id lookup
        var tickers = records.Select(r => r.Ticker).Distinct().ToHashSet();
        var assets = await _context.Assets
            .Where(a => tickers.Contains(a.Ticker))
            .ToDictionaryAsync(a => a.Ticker, a => a.Id, ct);

        foreach (var record in records)
        {
            if (!assets.TryGetValue(record.Ticker, out Guid assetId)) continue;

            await _assetPriceRepository.UpsertAsync(new AssetPrice
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                TradingDate = record.TradingDate,
                ClosingPrice = record.ClosingPrice
            });
        }
    }
}

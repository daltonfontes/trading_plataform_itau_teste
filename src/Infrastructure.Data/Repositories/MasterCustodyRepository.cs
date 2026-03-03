using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class MasterCustodyRepository : IMasterCustodyRepository
{
    private readonly DataContext _context;

    public MasterCustodyRepository(DataContext context) => _context = context;

    public async Task<List<MasterCustodyItem>> GetAllAsync() =>
        await _context.MasterCustodyItems
            .Include(m => m.Asset)
            .ToListAsync();

    public async Task<MasterCustodyItem?> GetByAssetIdAsync(Guid assetId) =>
        await _context.MasterCustodyItems
            .Include(m => m.Asset)
            .FirstOrDefaultAsync(m => m.AssetId == assetId);

    public async Task UpsertAsync(MasterCustodyItem item)
    {
        var existing = await _context.MasterCustodyItems.FindAsync(item.Id);
        if (existing is null)
            _context.MasterCustodyItems.Add(item);
        else
            _context.MasterCustodyItems.Update(item);

        await _context.SaveChangesAsync();
    }
}

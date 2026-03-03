using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class CustomerCustodyRepository : ICustomerCustodyRepository
{
    private readonly DataContext _context;

    public CustomerCustodyRepository(DataContext context) => _context = context;

    public async Task<List<CustomerCustodyItem>> GetByCustomerIdAsync(Guid customerId) =>
        await _context.CustomerCustodyItems
            .Include(c => c.Asset)
            .Where(c => c.CustomerId == customerId)
            .ToListAsync();

    public async Task<CustomerCustodyItem?> GetByCustomerAndAssetAsync(Guid customerId, Guid assetId) =>
        await _context.CustomerCustodyItems
            .Include(c => c.Asset)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.AssetId == assetId);

    public async Task<List<CustomerCustodyItem>> GetAllByAssetIdAsync(Guid assetId) =>
        await _context.CustomerCustodyItems
            .Include(c => c.Asset)
            .Where(c => c.AssetId == assetId)
            .ToListAsync();

    public async Task UpsertAsync(CustomerCustodyItem item)
    {
        var existing = await _context.CustomerCustodyItems.FindAsync(item.Id);
        if (existing is null)
            _context.CustomerCustodyItems.Add(item);
        else
            _context.CustomerCustodyItems.Update(item);

        await _context.SaveChangesAsync();
    }
}

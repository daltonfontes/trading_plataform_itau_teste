using Domain.Entities;

namespace Domain.Interfaces;

public interface IMasterCustodyRepository
{
    Task<List<MasterCustodyItem>> GetAllAsync();
    Task<MasterCustodyItem?> GetByAssetIdAsync(Guid assetId);
    Task UpsertAsync(MasterCustodyItem item);
}

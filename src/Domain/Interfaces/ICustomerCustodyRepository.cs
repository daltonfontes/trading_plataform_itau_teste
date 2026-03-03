using Domain.Entities;

namespace Domain.Interfaces;

public interface ICustomerCustodyRepository
{
    Task<List<CustomerCustodyItem>> GetByCustomerIdAsync(Guid customerId);
    Task<CustomerCustodyItem?> GetByCustomerAndAssetAsync(Guid customerId, Guid assetId);
    Task UpsertAsync(CustomerCustodyItem item);
    Task<List<CustomerCustodyItem>> GetAllByAssetIdAsync(Guid assetId);
}

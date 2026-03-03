namespace Domain.Entities;

public class MasterCustodyItem
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }

    public Asset Asset { get; set; } = null!;
}

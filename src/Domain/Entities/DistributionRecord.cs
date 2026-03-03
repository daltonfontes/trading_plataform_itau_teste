namespace Domain.Entities;

public class DistributionRecord
{
    public Guid Id { get; set; }
    public Guid BuyCycleId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid AssetId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalValue { get; set; }
    public decimal IrTax { get; set; }
    public DateTime DistributedAt { get; set; }

    public Asset Asset { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}

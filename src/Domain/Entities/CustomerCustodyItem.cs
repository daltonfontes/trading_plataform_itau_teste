namespace Domain.Entities;

public class CustomerCustodyItem
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid AssetId { get; set; }
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }

    public Asset Asset { get; set; } = null!;
}

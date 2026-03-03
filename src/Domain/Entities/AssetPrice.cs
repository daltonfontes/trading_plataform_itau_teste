namespace Domain.Entities;

public class AssetPrice
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public DateTime TradingDate { get; set; }
    public decimal ClosingPrice { get; set; }

    public Asset Asset { get; set; } = null!;
}

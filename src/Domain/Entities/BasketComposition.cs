namespace Domain.Entities;

public class BasketComposition
{
    public Guid Id { get; set; }
    public Guid TopFiveBasketId { get; set; }
    public Guid AssetId { get; set; }
    public decimal Percentage { get; set; }

    public Asset Asset { get; set; } = null!;
}

using Domain.Enums;

namespace Domain.Entities;

public class TopFiveBasket
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime ActiveSince { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public BasketStatus Status { get; set; } = BasketStatus.Active;
    public List<BasketComposition> Compositions { get; set; } = [];
}

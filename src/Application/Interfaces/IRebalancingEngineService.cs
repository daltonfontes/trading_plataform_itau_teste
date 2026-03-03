using Domain.Entities;

namespace Application.Interfaces;

public interface IRebalancingEngineService
{
    Task RebalanceOnBasketChangeAsync(TopFiveBasket newBasket, TopFiveBasket previousBasket, CancellationToken ct = default);
    Task RebalanceOnDeviationAsync(CancellationToken ct = default);
    Task RebalanceOnDeviationAsync(TopFiveBasket basket, CancellationToken ct = default);
}

using Domain.Entities;

namespace Domain.Interfaces;

public interface ITopFiveBasketRepository
{
    Task<TopFiveBasket?> GetActiveAsync();
    Task<List<TopFiveBasket>> GetAllAsync();
    Task AddAsync(TopFiveBasket basket);
    Task UpdateAsync(TopFiveBasket basket);
}

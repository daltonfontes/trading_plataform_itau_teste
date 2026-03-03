using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class TopFiveBasketRepository : ITopFiveBasketRepository
{
    private readonly DataContext _context;

    public TopFiveBasketRepository(DataContext context) => _context = context;

    public async Task<TopFiveBasket?> GetActiveAsync() =>
        await _context.TopFiveBaskets
            .Include(b => b.Compositions)
                .ThenInclude(c => c.Asset)
            .FirstOrDefaultAsync(b => b.Status == BasketStatus.Active);

    public async Task<List<TopFiveBasket>> GetAllAsync() =>
        await _context.TopFiveBaskets
            .Include(b => b.Compositions)
                .ThenInclude(c => c.Asset)
            .OrderByDescending(b => b.ActiveSince)
            .ToListAsync();

    public async Task AddAsync(TopFiveBasket basket)
    {
        _context.TopFiveBaskets.Add(basket);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TopFiveBasket basket)
    {
        _context.TopFiveBaskets.Update(basket);
        await _context.SaveChangesAsync();
    }
}

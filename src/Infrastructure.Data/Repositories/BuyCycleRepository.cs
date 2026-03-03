using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class BuyCycleRepository : IBuyCycleRepository
{
    private readonly DataContext _context;

    public BuyCycleRepository(DataContext context) => _context = context;

    public async Task<bool> AlreadyExecutedAsync(int year, int month, Installment installment) =>
        await _context.BuyCycles.AnyAsync(c =>
            c.Date.Year == year &&
            c.Date.Month == month &&
            c.Installment == installment &&
            c.Status == CycleStatus.Executed);

    public async Task AddAsync(BuyCycle cycle)
    {
        _context.BuyCycles.Add(cycle);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(BuyCycle cycle)
    {
        _context.BuyCycles.Update(cycle);
        await _context.SaveChangesAsync();
    }
}

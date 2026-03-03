using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces;

public interface IBuyCycleRepository
{
    Task<bool> AlreadyExecutedAsync(int year, int month, Installment installment);
    Task AddAsync(BuyCycle cycle);
    Task UpdateAsync(BuyCycle cycle);
}

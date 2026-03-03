using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly DataContext _context;

    public CustomerRepository(DataContext context) => _context = context;

    public async Task<List<Customer>> GetAllActiveAsync() =>
        await _context.Customers
            .Where(c => c.Status == CustomerStatus.Active)
            .ToListAsync();

    public async Task<Customer?> GetByIdAsync(Guid id) =>
        await _context.Customers.FindAsync(id);

    public async Task<Customer?> GetByCpfAsync(string cpf) =>
        await _context.Customers.FirstOrDefaultAsync(c => c.CPF == cpf);

    public async Task AddAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Customer customer)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
    }
}

using Domain.Entities;

namespace Domain.Interfaces;

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllActiveAsync();
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer?> GetByCpfAsync(string cpf);
    Task AddAsync(Customer customer);
    Task UpdateAsync(Customer customer);
}

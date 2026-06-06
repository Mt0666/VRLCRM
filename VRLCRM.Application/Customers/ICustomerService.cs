using VRLCRM.Domain.Entities;

namespace VRLCRM.Application.Customers;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Customer> CreateAsync(Customer customer, Address address, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Customer customer, Address address, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

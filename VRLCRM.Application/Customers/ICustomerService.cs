using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Application.Customers;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetOrdersAsync(int customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Invoice>> GetSalesInvoicesAsync(int customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> GetPaymentsAsync(int customerId, CancellationToken cancellationToken = default);

    Task<Customer?> GetByIdWithHistoryAsync(int id, CancellationToken cancellationToken = default);

    Task<Customer> CreateAsync(Customer customer, Address address, string? loginPhone = null, string? password = null, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Customer customer, Address address, string? loginPhone = null, string? password = null, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
}

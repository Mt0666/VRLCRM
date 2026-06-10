using VRLCRM.Domain.Entities;

namespace VRLCRM.Application.Suppliers;

public interface ISupplierService
{
    Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Supplier> CreateAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

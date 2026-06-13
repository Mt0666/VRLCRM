using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Application.Suppliers;

public interface ISupplierService
{
    Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Supplier> CreateAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Invoice>> GetPurchaseInvoicesAsync(int supplierId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
}

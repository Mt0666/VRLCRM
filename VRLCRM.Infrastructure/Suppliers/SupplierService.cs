using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Suppliers;
using VRLCRM.Domain.Entities;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Infrastructure.Suppliers;

public class SupplierService : ISupplierService
{
    private readonly ApplicationDbContext _context;

    public SupplierService(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .OrderByDescending(s => s.IsActive)
            .ThenByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .Include(s => s.PurchaseInvoices)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Supplier> CreateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        supplier.IsActive = true;
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(cancellationToken);
        return supplier;
    }

    public async Task<bool> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == supplier.Id, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        existing.CompanyName = supplier.CompanyName;
        existing.ContactName = supplier.ContactName;
        existing.PhoneNumber = supplier.PhoneNumber;
        existing.TaxNumber = supplier.TaxNumber;
        existing.Notes = supplier.Notes;
        existing.CreditLimit = supplier.CreditLimit;
        existing.City = supplier.City;
        existing.District = supplier.District;
        existing.AddressLine = supplier.AddressLine;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (supplier is null || !supplier.IsActive)
        {
            return false;
        }

        supplier.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

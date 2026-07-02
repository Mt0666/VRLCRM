using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Stocks;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Infrastructure.Stocks;

public class StockService : IStockService
{
    private readonly ApplicationDbContext _context;

    public StockService(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<IReadOnlyList<StockItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.StockItems
            .AsNoTracking()
            .Include(s => s.Category)
            .OrderByDescending(s => s.IsActive)
            .ThenByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.StockItems
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<StockMovementHistory> GetMovementHistoryAsync(
        int stockItemId,
        CancellationToken cancellationToken = default)
    {
        var purchaseRows = await _context.InvoiceLines
            .AsNoTracking()
            .Where(l => l.StockItemId == stockItemId
                        && l.IsActive
                        && l.Invoice.IsActive
                        && l.Invoice.InvoiceType == InvoiceType.Purchase)
            .OrderByDescending(l => l.Invoice.InvoiceDate)
            .Select(l => new
            {
                l.Invoice.InvoiceDate,
                l.Invoice.InvoiceNumber,
                SupplierCompany = l.Invoice.Supplier != null ? l.Invoice.Supplier.CompanyName : null,
                l.Quantity,
                l.UnitPrice,
                l.LineTotal
            })
            .ToListAsync(cancellationToken);

        var salesRows = await _context.InvoiceLines
            .AsNoTracking()
            .Where(l => l.StockItemId == stockItemId
                        && l.IsActive
                        && l.Invoice.IsActive
                        && l.Invoice.InvoiceType == InvoiceType.Sales)
            .OrderByDescending(l => l.Invoice.InvoiceDate)
            .Select(l => new
            {
                l.Invoice.InvoiceDate,
                l.Invoice.InvoiceNumber,
                CustomerCompany = l.Invoice.Customer != null ? l.Invoice.Customer.CompanyName : null,
                CustomerFirstName = l.Invoice.Customer != null ? l.Invoice.Customer.FirstName : null,
                CustomerLastName = l.Invoice.Customer != null ? l.Invoice.Customer.LastName : null,
                l.Quantity,
                l.UnitPrice,
                l.LineTotal
            })
            .ToListAsync(cancellationToken);

        var purchases = purchaseRows
            .Select(r => new StockMovementRow
            {
                Date = r.InvoiceDate,
                DocumentNumber = r.InvoiceNumber,
                PartyName = string.IsNullOrWhiteSpace(r.SupplierCompany) ? "-" : r.SupplierCompany,
                Quantity = r.Quantity,
                UnitPrice = r.UnitPrice,
                LineTotal = r.LineTotal
            })
            .ToList();

        var sales = salesRows
            .Select(r =>
            {
                var fullName = $"{r.CustomerFirstName} {r.CustomerLastName}".Trim();
                var party = !string.IsNullOrWhiteSpace(r.CustomerCompany)
                    ? r.CustomerCompany
                    : (string.IsNullOrWhiteSpace(fullName) ? "-" : fullName);
                return new StockMovementRow
                {
                    Date = r.InvoiceDate,
                    DocumentNumber = r.InvoiceNumber,
                    PartyName = party,
                    Quantity = r.Quantity,
                    UnitPrice = r.UnitPrice,
                    LineTotal = r.LineTotal
                };
            })
            .ToList();

        return new StockMovementHistory { Purchases = purchases, Sales = sales };
    }

    public async Task<bool> StockCodeExistsAsync(
        string stockCode,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = stockCode.Trim();
        return await _context.StockItems.AnyAsync(
            s => s.StockCode == normalized && (!excludeId.HasValue || s.Id != excludeId.Value),
            cancellationToken);
    }

    public async Task<StockItem> CreateAsync(StockItem stockItem, CancellationToken cancellationToken = default)
    {
        stockItem.IsActive = true;
        _context.StockItems.Add(stockItem);
        await _context.SaveChangesAsync(cancellationToken);
        return stockItem;
    }

    public async Task<bool> UpdateAsync(StockItem stockItem, CancellationToken cancellationToken = default)
    {
        var existing = await _context.StockItems
            .FirstOrDefaultAsync(s => s.Id == stockItem.Id, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        var oldQuantity = existing.StockQuantity;
        var newQuantity = stockItem.StockQuantity;

        existing.StockCode = stockItem.StockCode;
        existing.Barcode = stockItem.Barcode;
        existing.CategoryId = stockItem.CategoryId;
        existing.ImageUrl = stockItem.ImageUrl;
        existing.ThumbnailUrl = stockItem.ThumbnailUrl;
        existing.Name = stockItem.Name;
        existing.Price = stockItem.Price;
        existing.VatRate = stockItem.VatRate;
        existing.StockQuantity = newQuantity;
        existing.CriticalStockLevel = stockItem.CriticalStockLevel;
        existing.Description = stockItem.Description;

        if (oldQuantity != newQuantity)
        {
            var diff = newQuantity - oldQuantity;
            _context.StockMovements.Add(new StockMovement
            {
                StockItemId = existing.Id,
                MovementType = diff > 0 ? StockMovementType.In : StockMovementType.Out,
                Quantity = Math.Abs(diff),
                ReferenceType = StockMovementReferenceType.Manual,
                ReferenceId = existing.Id,
                MovementDate = DateTime.UtcNow,
                Notes = "Manuel stok düzeltmesi"
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var stockItem = await _context.StockItems
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (stockItem is null)
        {
            return false;
        }

        if (!stockItem.IsActive)
        {
            return false;
        }

        stockItem.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        var stockItem = await _context.StockItems
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (stockItem is null || stockItem.IsActive)
            return false;

        stockItem.IsActive = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

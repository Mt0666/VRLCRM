using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Stocks;
using VRLCRM.Domain.Entities;
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
            .ThenByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.StockItems
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
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

        existing.StockCode = stockItem.StockCode;
        existing.Barcode = stockItem.Barcode;
        existing.CategoryId = stockItem.CategoryId;
        existing.ImageUrl = stockItem.ImageUrl;
        existing.Name = stockItem.Name;
        existing.Price = stockItem.Price;
        existing.StockQuantity = stockItem.StockQuantity;
        existing.Description = stockItem.Description;

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
}

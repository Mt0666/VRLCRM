using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Categories;
using VRLCRM.Domain.Entities;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Infrastructure.Categories;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderByDescending(c => c.IsActive)
            .ThenByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(
        string name,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim();
        return await _context.Categories.AnyAsync(
            c => c.Name == normalized && (!excludeId.HasValue || c.Id != excludeId.Value),
            cancellationToken);
    }

    public async Task<bool> HasStockItemsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.StockItems.AnyAsync(s => s.IsActive && s.CategoryId == id, cancellationToken);
    }

    public async Task<Category> CreateAsync(Category category, CancellationToken cancellationToken = default)
    {
        category.IsActive = true;
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<bool> UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == category.Id, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        existing.Name = category.Name;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
        {
            return false;
        }

        if (await HasStockItemsAsync(id, cancellationToken))
        {
            return false;
        }

        if (!category.IsActive)
        {
            return false;
        }

        category.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

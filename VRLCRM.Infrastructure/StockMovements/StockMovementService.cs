using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.StockMovements;
using VRLCRM.Domain.Entities;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Infrastructure.StockMovements;

public class StockMovementService : IStockMovementService
{
    private readonly ApplicationDbContext _context;

    public StockMovementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<StockMovement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.StockMovements
            .AsNoTracking()
            .Include(m => m.StockItem)
            .OrderByDescending(m => m.IsActive)
            .ThenByDescending(m => m.UpdatedAt ?? m.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

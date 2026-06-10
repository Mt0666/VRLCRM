using VRLCRM.Domain.Entities;

namespace VRLCRM.Application.StockMovements;

public interface IStockMovementService
{
    Task<IReadOnlyList<StockMovement>> GetAllAsync(CancellationToken cancellationToken = default);
}

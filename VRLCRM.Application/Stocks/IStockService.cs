using VRLCRM.Domain.Entities;

namespace VRLCRM.Application.Stocks;

public interface IStockService
{
    Task<IReadOnlyList<StockItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<StockItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> StockCodeExistsAsync(string stockCode, int? excludeId = null, CancellationToken cancellationToken = default);

    Task<StockItem> CreateAsync(StockItem stockItem, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(StockItem stockItem, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

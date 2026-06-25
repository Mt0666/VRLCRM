using VRLCRM.Domain.Entities;

namespace VRLCRM.Application.Orders;

public class OrderLineInput
{
    public int StockItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? VatRate { get; set; }

    public string? Notes { get; set; }
}

public interface IOrderService
{
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<StockItem?> FindByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    Task<Order> CreateAndApproveAsync(
        int? customerId,
        int? supplierId,
        string? notes,
        decimal discountRate,
        IReadOnlyList<OrderLineInput> lines,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        int id,
        decimal discountRate,
        IReadOnlyList<OrderLineInput> lines,
        CancellationToken cancellationToken = default);

    Task<bool> CancelAsync(int id, CancellationToken cancellationToken = default);
}

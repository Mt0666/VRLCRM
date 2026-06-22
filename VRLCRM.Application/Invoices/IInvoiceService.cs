using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Application.Invoices;

public class NewPurchaseProductInput
{
    public string StockCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string? NewCategoryName { get; set; }

    public string? Barcode { get; set; }

    public decimal VatRate { get; set; } = 20m;

    public int CriticalStockLevel { get; set; } = 5;
}

public class InvoiceLineInput
{
    public int? StockItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    /// <summary>Alış faturasında kullanıcının belirlediği satış fiyatı. Null ise UnitPrice × 1.30 kullanılır.</summary>
    public decimal? SalePrice { get; set; }

    public NewPurchaseProductInput? NewProduct { get; set; }
}

public class InvoiceLineUpdateInput
{
    public int StockItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal VatRate { get; set; }
}

public interface IInvoiceService
{
    Task<IReadOnlyList<Invoice>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Invoice>> GetByTypeAsync(InvoiceType invoiceType, CancellationToken cancellationToken = default);

    Task<Invoice?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Invoice> CreateAsync(
        InvoiceType invoiceType,
        int? customerId,
        int? supplierId,
        DateTime invoiceDate,
        string? notes,
        IReadOnlyList<InvoiceLineInput> lines,
        decimal discountRate = 0m,
        CancellationToken cancellationToken = default);

    Task<Invoice> CreateSalesInvoiceFromOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<bool> UpdateSalesInvoiceAsync(
        int id,
        decimal discountRate,
        IReadOnlyList<InvoiceLineUpdateInput> lines,
        CancellationToken cancellationToken = default);

    Task<bool> UpdatePurchaseInvoiceAsync(
        int id,
        decimal discountRate,
        IReadOnlyList<InvoiceLineUpdateInput> lines,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

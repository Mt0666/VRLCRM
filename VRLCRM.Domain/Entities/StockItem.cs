using VRLCRM.Domain.Common;

namespace VRLCRM.Domain.Entities;

public class StockItem : BaseEntity
{
    public string StockCode { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Son alış faturasından gelen maliyet fiyatı.</summary>
    public decimal PurchasePrice { get; set; }

    /// <summary>Satış fiyatı. Alış faturası kaydedilince PurchasePrice × 1.30 olarak otomatik güncellenir.</summary>
    public decimal Price { get; set; }

    public decimal VatRate { get; set; } = 20m;

    public int StockQuantity { get; set; }

    /// <summary>Stok bu seviyenin altına düştüğünde kritik kabul edilir.</summary>
    public int CriticalStockLevel { get; set; } = 5;

    public string? Description { get; set; }

    public bool IsCritical => StockQuantity <= CriticalStockLevel;
}

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

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public string? Description { get; set; }
}

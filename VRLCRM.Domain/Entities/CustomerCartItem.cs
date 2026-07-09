namespace VRLCRM.Domain.Entities;

/// <summary>B2B müşteri sepeti — kalıcı (veritabanında müşteriye bağlı) tutulur.</summary>
public class CustomerCartItem
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int StockItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public string? Notes { get; set; }
}

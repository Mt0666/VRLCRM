using VRLCRM.Domain.Common;

namespace VRLCRM.Domain.Entities;

public class OrderLine : BaseEntity
{
    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public int StockItemId { get; set; }

    public StockItem StockItem { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal VatRate { get; set; }

    public decimal VatAmount { get; set; }

    public decimal LineTotal { get; set; }
}

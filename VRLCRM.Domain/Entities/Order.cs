using VRLCRM.Domain.Common;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; }

    public decimal SubTotal { get; set; }

    public decimal VatTotal { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public int? SalesInvoiceId { get; set; }

    public Invoice? SalesInvoice { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = [];
}

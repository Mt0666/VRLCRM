using VRLCRM.Domain.Common;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public int? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public int? SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; }

    public decimal SubTotal { get; set; }

    public decimal VatTotal { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountRate { get; set; }

    public decimal GrossTotal => SubTotal + VatTotal;

    public decimal DiscountAmount => GrossTotal * DiscountRate / 100m;

    public string? Notes { get; set; }

    public int? SalesInvoiceId { get; set; }

    public Invoice? SalesInvoice { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = [];

    public string StatusLabel
    {
        get
        {
            if (Status == OrderStatus.Cancelled || !IsActive)
            {
                return "İptal";
            }

            if (SalesInvoiceId.HasValue)
            {
                return "Faturalandırıldı";
            }

            return "Beklemede";
        }
    }
}

using VRLCRM.Domain.Common;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public InvoiceType InvoiceType { get; set; }

    public DateTime InvoiceDate { get; set; }

    public int? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public int? SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public decimal SubTotal { get; set; }

    public decimal VatTotal { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = [];
}

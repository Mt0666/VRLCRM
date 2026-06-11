using VRLCRM.Domain.Common;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Domain.Entities;

public class Payment : BaseEntity
{
    public string PaymentNumber { get; set; } = string.Empty;

    public PaymentType Type { get; set; }

    public PaymentMethod Method { get; set; }

    public DateTime PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public string? Notes { get; set; }
}

using VRLCRM.Domain.Common;

namespace VRLCRM.Domain.Entities;

public class Supplier : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;

    public string? ContactName { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public string? TaxNumber { get; set; }

    public string? Notes { get; set; }

    /// <summary>Tedarikçiye olan borç bakiyesi (TL).</summary>
    public decimal Balance { get; set; }

    public decimal? CreditLimit { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? AddressLine { get; set; }

    public ICollection<Invoice> PurchaseInvoices { get; set; } = [];

    public decimal? AvailableCredit => CreditLimit.HasValue ? CreditLimit.Value - Balance : null;
}

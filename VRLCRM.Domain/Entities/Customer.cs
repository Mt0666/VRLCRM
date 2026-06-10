using VRLCRM.Domain.Common;

namespace VRLCRM.Domain.Entities;

public class Customer : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? CompanyName { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Notes { get; set; }

    /// <summary>Müşterinin güncel borç bakiyesi (TL).</summary>
    public decimal Balance { get; set; }

    /// <summary>Null ise limit yok; aksi halde Balance + yeni sipariş bu limiti aşamaz.</summary>
    public decimal? CreditLimit { get; set; }

    public Address? Address { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = [];

    public ICollection<Order> Orders { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}".Trim();

    public decimal? AvailableCredit => CreditLimit.HasValue ? CreditLimit.Value - Balance : null;

    public bool CanPlaceOrder(decimal orderTotal)
    {
        if (!CreditLimit.HasValue)
        {
            return true;
        }

        return Balance + orderTotal <= CreditLimit.Value;
    }
}

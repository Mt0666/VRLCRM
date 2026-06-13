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

    /// <summary>Boş bırakılırsa 0 kabul edilir; Balance + yeni sipariş bu limiti aşamaz.</summary>
    public decimal? CreditLimit { get; set; }

    public Address? Address { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = [];

    public ICollection<Order> Orders { get; set; } = [];

    public ICollection<Payment> Payments { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}".Trim();

    public decimal EffectiveCreditLimit => CreditLimit ?? 0;

    public decimal AvailableCredit => EffectiveCreditLimit - Balance;

    public bool HasSufficientCredit(decimal amountToAdd) =>
        Balance + amountToAdd <= EffectiveCreditLimit;
}

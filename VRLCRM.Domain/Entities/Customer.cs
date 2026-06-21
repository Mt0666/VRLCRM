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

    /// <summary>null veya 0 ise müşterinin limiti sınırsızdır.</summary>
    public bool IsUnlimitedCredit => !CreditLimit.HasValue || CreditLimit.Value == 0;

    public decimal EffectiveCreditLimit => CreditLimit ?? 0;

    public decimal AvailableCredit => IsUnlimitedCredit ? decimal.MaxValue : CreditLimit!.Value - Balance;

    public bool HasSufficientCredit(decimal amountToAdd) =>
        IsUnlimitedCredit || Balance + amountToAdd <= CreditLimit!.Value;
}

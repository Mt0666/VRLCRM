namespace VRLCRM.Models.Customers;

public class CustomerTransactionRow
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    /// <summary>true = direkt satış faturası, false = sipariş</summary>
    public bool IsDirectInvoice { get; set; }

    /// <summary>Sipariş durumu etiketi veya "Faturalandırıldı"</summary>
    public string StatusLabel { get; set; } = string.Empty;

    public string StatusColor { get; set; } = "secondary";

    public decimal TotalAmount { get; set; }
}

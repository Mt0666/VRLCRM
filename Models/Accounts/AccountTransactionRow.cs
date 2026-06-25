namespace VRLCRM.Models.Accounts;

public enum AccountTransactionKind
{
    Order,
    SalesInvoice,
    PurchaseInvoice,
    PaymentIncoming,
    PaymentOutgoing
}

public class AccountTransactionRow
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public AccountTransactionKind Kind { get; set; }
    public string TypeLabel { get; set; } = string.Empty;
    public string TypeBadgeColor { get; set; } = "secondary";
    public string? StatusLabel { get; set; }
    public string? StatusColor { get; set; }
    public decimal Amount { get; set; }
    public string BalanceEffectLabel { get; set; } = "—";
    public string BalanceEffectColor { get; set; } = "secondary";
    public string DetailController { get; set; } = string.Empty;
    public string DetailAction { get; set; } = "Details";
}

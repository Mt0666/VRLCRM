using VRLCRM.Domain.Entities;

namespace VRLCRM.Models.Dashboards;

public class DashboardViewModel
{
    // ── KPI kartları ─────────────────────────────────────────────
    public decimal ThisMonthSales { get; set; }
    public decimal ThisMonthPurchases { get; set; }
    public decimal TotalReceivables { get; set; }
    public decimal TotalPayables { get; set; }
    public int PendingOrdersCount { get; set; }
    public int UnbilledApprovedOrdersCount { get; set; }

    // ── Sayaçlar ─────────────────────────────────────────────────
    public int TotalCustomers { get; set; }
    public int TotalStockItems { get; set; }
    public int CriticalStockCount { get; set; }

    // ── Grafik: son 6 ay satış & alış ────────────────────────────
    public List<MonthlyTrendItem> Last6MonthsTrend { get; set; } = new();

    // ── Grafik: bu ay tahsilat yöntemi dağılımı ──────────────────
    public List<PaymentMethodItem> ThisMonthPaymentMethods { get; set; } = new();

    // ── Listeler ─────────────────────────────────────────────────
    public IReadOnlyList<Customer> TopDebtorCustomers { get; set; } = new List<Customer>();
    public IReadOnlyList<Order> RecentOrders { get; set; } = new List<Order>();
    public IReadOnlyList<StockItem> CriticalStocks { get; set; } = new List<StockItem>();
}

public class MonthlyTrendItem
{
    public string Month { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public decimal Purchases { get; set; }
}

public class PaymentMethodItem
{
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

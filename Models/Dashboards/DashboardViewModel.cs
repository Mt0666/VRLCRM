using VRLCRM.Domain.Entities;

namespace VRLCRM.Models.Dashboards;

public class DashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int TotalSuppliers { get; set; }
    public int TotalStockItems { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal TotalPurchasesAmount { get; set; }
    public decimal TotalOrdersAmount { get; set; }

    public IReadOnlyList<Order> RecentOrders { get; set; } = new List<Order>();
    public IReadOnlyList<StockItem> CriticalStocks { get; set; } = new List<StockItem>();
}

using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VRLCRM.Application.Customers;
using VRLCRM.Application.Invoices;
using VRLCRM.Application.Orders;
using VRLCRM.Application.Payments;
using VRLCRM.Application.Stocks;
using VRLCRM.Application.Suppliers;
using VRLCRM.Domain.Constants;
using VRLCRM.Domain.Enums;
using VRLCRM.Models.Dashboards;

namespace VRLCRM.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class DashboardsController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly ISupplierService _supplierService;
    private readonly IStockService _stockService;
    private readonly IOrderService _orderService;
    private readonly IInvoiceService _invoiceService;
    private readonly IPaymentService _paymentService;

    public DashboardsController(
        ICustomerService customerService,
        ISupplierService supplierService,
        IStockService stockService,
        IOrderService orderService,
        IInvoiceService invoiceService,
        IPaymentService paymentService)
    {
        _customerService = customerService;
        _supplierService = supplierService;
        _stockService = stockService;
        _orderService = orderService;
        _invoiceService = invoiceService;
        _paymentService = paymentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var customers        = await _customerService.GetAllAsync(cancellationToken);
        var suppliers        = await _supplierService.GetAllAsync(cancellationToken);
        var stocks           = await _stockService.GetAllAsync(cancellationToken);
        var orders           = await _orderService.GetAllAsync(cancellationToken);
        var salesInvoices    = await _invoiceService.GetByTypeAsync(InvoiceType.Sales, cancellationToken);
        var purchaseInvoices = await _invoiceService.GetByTypeAsync(InvoiceType.Purchase, cancellationToken);
        var payments         = await _paymentService.GetAllAsync(cancellationToken);

        var now            = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var trCulture      = new CultureInfo("tr-TR");

        // ── KPI: bu ay ───────────────────────────────────────────────
        var activeSales     = salesInvoices.Where(i => i.IsActive).ToList();
        var activePurchases = purchaseInvoices.Where(i => i.IsActive).ToList();
        var activeOrders    = orders.Where(o => o.IsActive).ToList();

        var thisMonthSales     = activeSales.Where(i => i.InvoiceDate >= thisMonthStart).Sum(i => i.TotalAmount);
        var thisMonthPurchases = activePurchases.Where(i => i.InvoiceDate >= thisMonthStart).Sum(i => i.TotalAmount);

        // ── KPI: alacak / borç bakiyeleri ────────────────────────────
        var activeCustomers = customers.Where(c => c.IsActive).ToList();
        var activeSuppliers = suppliers.Where(s => s.IsActive).ToList();

        var totalReceivables = activeCustomers.Where(c => c.Balance > 0).Sum(c => c.Balance);
        var totalPayables    = activeSuppliers.Where(s => s.Balance > 0).Sum(s => s.Balance);

        // ── KPI: bekleyen & faturasız siparişler ─────────────────────
        var pendingOrdersCount          = activeOrders.Count(o => o.Status == OrderStatus.Pending);
        var unbilledApprovedOrdersCount = activeOrders.Count(o => o.Status == OrderStatus.Approved && o.SalesInvoiceId == null);

        // ── Grafik: son 6 ay satış & alış trendi ─────────────────────
        var trend = Enumerable.Range(0, 6)
            .Select(i => thisMonthStart.AddMonths(-5 + i))
            .Select(monthStart =>
            {
                var monthEnd = monthStart.AddMonths(1);
                return new MonthlyTrendItem
                {
                    Month     = monthStart.ToString("MMM yy", trCulture),
                    Sales     = activeSales.Where(inv => inv.InvoiceDate >= monthStart && inv.InvoiceDate < monthEnd).Sum(inv => inv.TotalAmount),
                    Purchases = activePurchases.Where(inv => inv.InvoiceDate >= monthStart && inv.InvoiceDate < monthEnd).Sum(inv => inv.TotalAmount)
                };
            })
            .ToList();

        // ── Grafik: bu ay tahsilat yöntemi dağılımı ──────────────────
        var methodLabels = new Dictionary<PaymentMethod, string>
        {
            [PaymentMethod.Cash]         = "Nakit",
            [PaymentMethod.BankTransfer] = "Havale / EFT",
            [PaymentMethod.CreditCard]   = "Kredi Kartı",
            [PaymentMethod.Check]        = "Çek"
        };

        var paymentMethods = payments
            .Where(p => p.IsActive && p.Type == PaymentType.Incoming && p.PaymentDate >= thisMonthStart)
            .GroupBy(p => p.Method)
            .Select(g => new PaymentMethodItem
            {
                Method = methodLabels.TryGetValue(g.Key, out var label) ? label : g.Key.ToString(),
                Amount = g.Sum(p => p.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        // ── Liste: en yüksek bakiyeli müşteriler ─────────────────────
        var topDebtors = activeCustomers
            .Where(c => c.Balance > 0)
            .OrderByDescending(c => c.Balance)
            .Take(5)
            .ToList();

        // ── Liste: kritik stok & son siparişler ──────────────────────
        var activeStocks  = stocks.Where(s => s.IsActive).ToList();
        var criticalItems = activeStocks
            .Where(s => s.StockQuantity <= s.CriticalStockLevel)
            .OrderBy(s => s.StockQuantity)
            .ThenBy(s => s.Name)
            .ToList();
        var recentOrders  = activeOrders.OrderByDescending(o => o.OrderDate).Take(6).ToList();

        var model = new DashboardViewModel
        {
            ThisMonthSales              = thisMonthSales,
            ThisMonthPurchases          = thisMonthPurchases,
            TotalReceivables            = totalReceivables,
            TotalPayables               = totalPayables,
            PendingOrdersCount          = pendingOrdersCount,
            UnbilledApprovedOrdersCount = unbilledApprovedOrdersCount,

            TotalCustomers    = activeCustomers.Count,
            TotalStockItems   = activeStocks.Count,
            CriticalStockCount = criticalItems.Count,

            Last6MonthsTrend         = trend,
            ThisMonthPaymentMethods  = paymentMethods,

            TopDebtorCustomers = topDebtors,
            RecentOrders       = recentOrders,
            CriticalStocks     = criticalItems.Take(6).ToList(),
            CriticalStockChart = criticalItems
                .Take(8)
                .Select(s => new CriticalStockChartItem
                {
                    Name = s.Name.Length > 22 ? s.Name[..22] + "…" : s.Name,
                    Quantity = s.StockQuantity,
                    CriticalLevel = s.CriticalStockLevel
                })
                .ToList()
        };

        return View(model);
    }
}

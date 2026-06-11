using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VRLCRM.Application.Customers;
using VRLCRM.Application.Invoices;
using VRLCRM.Application.Orders;
using VRLCRM.Application.Stocks;
using VRLCRM.Application.Suppliers;
using VRLCRM.Domain.Enums;
using VRLCRM.Models.Dashboards;

namespace VRLCRM.Controllers;

[Authorize]
public class DashboardsController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly ISupplierService _supplierService;
    private readonly IStockService _stockService;
    private readonly IOrderService _orderService;
    private readonly IInvoiceService _invoiceService;

    public DashboardsController(
        ICustomerService customerService,
        ISupplierService supplierService,
        IStockService stockService,
        IOrderService orderService,
        IInvoiceService invoiceService)
    {
        _customerService = customerService;
        _supplierService = supplierService;
        _stockService = stockService;
        _orderService = orderService;
        _invoiceService = invoiceService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var customers = await _customerService.GetAllAsync(cancellationToken);
        var suppliers = await _supplierService.GetAllAsync(cancellationToken);
        var stocks = await _stockService.GetAllAsync(cancellationToken);
        var orders = await _orderService.GetAllAsync(cancellationToken);
        var salesInvoices = await _invoiceService.GetByTypeAsync(InvoiceType.Sales, cancellationToken);
        var purchaseInvoices = await _invoiceService.GetByTypeAsync(InvoiceType.Purchase, cancellationToken);

        var model = new DashboardViewModel
        {
            TotalCustomers = customers.Count(c => c.IsActive),
            TotalSuppliers = suppliers.Count(s => s.IsActive),
            TotalStockItems = stocks.Count(s => s.IsActive),
            TotalSalesAmount = salesInvoices.Where(i => i.IsActive).Sum(i => i.TotalAmount),
            TotalPurchasesAmount = purchaseInvoices.Where(i => i.IsActive).Sum(i => i.TotalAmount),
            TotalOrdersAmount = orders.Where(o => o.IsActive && o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
            RecentOrders = orders.Where(o => o.IsActive).OrderByDescending(o => o.OrderDate).Take(8).ToList(),
            CriticalStocks = stocks.Where(s => s.IsActive && s.StockQuantity < 10).OrderBy(s => s.StockQuantity).Take(8).ToList()
        };

        return View(model);
    }
}

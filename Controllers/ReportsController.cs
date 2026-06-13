using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VRLCRM.Infrastructure.Data;
using VRLCRM.Domain.Constants;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> SalesReport(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var start = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = endDate ?? DateTime.Today.AddDays(1).AddTicks(-1);

        var invoices = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.IsActive && i.InvoiceType == InvoiceType.Sales && i.InvoiceDate >= start && i.InvoiceDate <= end)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);

        ViewBag.StartDate = start.ToString("yyyy-MM-dd");
        ViewBag.EndDate = end.ToString("yyyy-MM-dd");
        
        return View(invoices);
    }

    public async Task<IActionResult> StockReport(CancellationToken cancellationToken)
    {
        var stocks = await _context.StockItems
            .AsNoTracking()
            .Include(s => s.Category)
            .Where(s => s.IsActive)
            .OrderBy(s => s.StockQuantity)
            .ToListAsync(cancellationToken);

        return View(stocks);
    }
}

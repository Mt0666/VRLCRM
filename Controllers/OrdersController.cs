using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VRLCRM.Application.Customers;
using VRLCRM.Application.Invoices;
using VRLCRM.Application.Orders;
using VRLCRM.Models.Orders;
using VRLCRM.Services;

namespace VRLCRM.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ICustomerService _customerService;
    private readonly IInvoiceService _invoiceService;
    private readonly OrderDocumentService _documentService;

    public OrdersController(
        IOrderService orderService,
        ICustomerService customerService,
        IInvoiceService invoiceService,
        OrderDocumentService documentService)
    {
        _orderService = orderService;
        _customerService = customerService;
        _invoiceService = invoiceService;
        _documentService = documentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetAllAsync(cancellationToken);
        return View(orders);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new OrderFormViewModel();
        await PopulateCustomersAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderFormViewModel model, CancellationToken cancellationToken)
    {
        await PopulateCustomersAsync(model, cancellationToken);

        if (model.Lines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "En az bir ürün ekleyin.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var lines = model.Lines.Select(l => new OrderLineInput
            {
                StockItemId = l.StockItemId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList();

            var order = await _orderService.CreateAndApproveAsync(
                model.CustomerId,
                model.Notes,
                lines,
                cancellationToken);

            TempData["SuccessMessage"] = $"Sipariş {order.OrderNumber} onaylandı.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> LookupBarcode(string barcode, CancellationToken cancellationToken)
    {
        var item = await _orderService.FindByBarcodeAsync(barcode, cancellationToken);
        if (item is null)
        {
            return NotFound(new { message = "Ürün bulunamadı." });
        }

        return Json(new
        {
            id = item.Id,
            name = item.Name,
            price = item.Price,
            vatRate = item.VatRate,
            stockQuantity = item.StockQuantity,
            stockCode = item.StockCode
        });
    }

    public async Task<IActionResult> ExportPdf(int id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var bytes = _documentService.GeneratePdf(order);
        return File(bytes, "application/pdf", $"{order.OrderNumber}.pdf");
    }

    public async Task<IActionResult> ExportExcel(int id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var bytes = _documentService.GenerateExcel(order);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{order.OrderNumber}.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        try
        {
            var cancelled = await _orderService.CancelAsync(id, cancellationToken);
            if (!cancelled)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Sipariş iptal edildi. Stok ve cari hesap güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertToSalesInvoice(int id, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _invoiceService.CreateSalesInvoiceFromOrderAsync(id, cancellationToken);
            TempData["SuccessMessage"] = $"Sipariş satış faturasına dönüştürüldü: {invoice.InvoiceNumber}";
            return RedirectToAction("Details", "Invoices", new { id = invoice.Id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task PopulateCustomersAsync(OrderFormViewModel model, CancellationToken cancellationToken)
    {
        var customers = await _customerService.GetAllAsync(cancellationToken);
        model.Customers = customers
            .Where(c => c.IsActive)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.FullName} (Borç: {c.Balance:N2} ₺)",
                Selected = c.Id == model.CustomerId
            });
    }
}

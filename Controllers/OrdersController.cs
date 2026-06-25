using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VRLCRM.Application.Customers;
using VRLCRM.Application.Invoices;
using VRLCRM.Application.Orders;
using VRLCRM.Application.Stocks;
using VRLCRM.Application.Suppliers;
using VRLCRM.Domain.Constants;
using VRLCRM.Helpers;
using VRLCRM.Models.Orders;
using VRLCRM.Services;

namespace VRLCRM.Controllers;

[Authorize(Roles = AppRoles.AdminAndPersonel)]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ICustomerService _customerService;
    private readonly ISupplierService _supplierService;
    private readonly IInvoiceService _invoiceService;
    private readonly IStockService _stockService;
    private readonly OrderDocumentService _documentService;

    public OrdersController(
        IOrderService orderService,
        ICustomerService customerService,
        ISupplierService supplierService,
        IInvoiceService invoiceService,
        IStockService stockService,
        OrderDocumentService documentService)
    {
        _orderService = orderService;
        _customerService = customerService;
        _supplierService = supplierService;
        _invoiceService = invoiceService;
        _stockService = stockService;
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
        await PopulateSelectListsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderFormViewModel model, CancellationToken cancellationToken)
    {
        await PopulateSelectListsAsync(model, cancellationToken);

        if (model.PartyType == "supplier")
        {
            model.CustomerId = null;
            if (!model.SupplierId.HasValue)
            {
                ModelState.AddModelError(nameof(model.SupplierId), "Tedarikçi seçilmelidir.");
            }
        }
        else
        {
            model.SupplierId = null;
            if (!model.CustomerId.HasValue)
            {
                ModelState.AddModelError(nameof(model.CustomerId), "Müşteri seçilmelidir.");
            }
        }

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
            var lines = MapLines(model.Lines);
            var order = await _orderService.CreateAndApproveAsync(
                model.CustomerId,
                model.SupplierId,
                model.Notes,
                model.DiscountRate,
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, OrderFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Lines.Count == 0)
        {
            TempData["ErrorMessage"] = "Siparişte en az bir ürün olmalıdır.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            var updated = await _orderService.UpdateAsync(id, model.DiscountRate, MapLines(model.Lines), cancellationToken);
            if (!updated)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Sipariş güncellendi.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> SearchProduct(string term, CancellationToken cancellationToken)
    {
        var stocks = await _stockService.GetAllAsync(cancellationToken);
        var results = stocks
            .Where(s => s.IsActive && (
                s.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.StockCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (s.Barcode != null && s.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase))))
            .Take(20)
            .Select(s => new
            {
                id = s.Id,
                name = s.Name,
                stockCode = s.StockCode,
                barcode = s.Barcode,
                purchasePrice = s.PurchasePrice,
                price = s.Price,
                vatRate = s.VatRate,
                stockQuantity = s.StockQuantity
            });
        return Json(results);
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
            purchasePrice = item.PurchasePrice,
            price = item.Price,
            vatRate = item.VatRate,
            stockQuantity = item.StockQuantity,
            stockCode = item.StockCode,
            barcode = item.Barcode
        });
    }

    public async Task<IActionResult> ExportPdf(int id, bool inline = false, CancellationToken cancellationToken = default)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var bytes = _documentService.GeneratePdf(order);
        if (inline)
        {
            return PdfFileResults.AsInline(bytes);
        }

        return PdfFileResults.AsDownload(bytes, $"{order.OrderNumber}.pdf");
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

            TempData["SuccessMessage"] = "Sipariş başarıyla iptal edildi.";
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

    private static List<OrderLineInput> MapLines(IEnumerable<OrderLineFormItem> lines) =>
        lines.Select(l => new OrderLineInput
        {
            StockItemId = l.StockItemId,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            VatRate = l.VatRate,
            Notes = l.Notes
        }).ToList();

    private async Task PopulateSelectListsAsync(OrderFormViewModel model, CancellationToken cancellationToken)
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

        var suppliers = await _supplierService.GetAllAsync(cancellationToken);
        model.Suppliers = suppliers
            .Where(s => s.IsActive)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.CompanyName} (Borç: {s.Balance:N2} ₺)",
                Selected = s.Id == model.SupplierId
            });
    }
}

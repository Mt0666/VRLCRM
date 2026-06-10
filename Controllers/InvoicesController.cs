using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VRLCRM.Application.Categories;
using VRLCRM.Application.Customers;
using VRLCRM.Application.Invoices;
using VRLCRM.Application.Stocks;
using VRLCRM.Application.Suppliers;
using VRLCRM.Domain.Enums;
using VRLCRM.Models.Invoices;
using VRLCRM.Services;

namespace VRLCRM.Controllers;

public class InvoicesController : Controller
{
    private readonly IInvoiceService _invoiceService;
    private readonly ICustomerService _customerService;
    private readonly IStockService _stockService;
    private readonly ICategoryService _categoryService;
    private readonly ISupplierService _supplierService;
    private readonly InvoiceDocumentService _documentService;

    public InvoicesController(
        IInvoiceService invoiceService,
        ICustomerService customerService,
        IStockService stockService,
        ICategoryService categoryService,
        ISupplierService supplierService,
        InvoiceDocumentService documentService)
    {
        _invoiceService = invoiceService;
        _customerService = customerService;
        _stockService = stockService;
        _categoryService = categoryService;
        _supplierService = supplierService;
        _documentService = documentService;
    }

    public IActionResult Index() => RedirectToAction(nameof(Sales));

    public async Task<IActionResult> Sales(CancellationToken cancellationToken)
    {
        var invoices = await _invoiceService.GetByTypeAsync(InvoiceType.Sales, cancellationToken);
        return View(invoices);
    }

    public async Task<IActionResult> Purchase(CancellationToken cancellationToken)
    {
        var invoices = await _invoiceService.GetByTypeAsync(InvoiceType.Purchase, cancellationToken);
        return View(invoices);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        return View(invoice);
    }

    public async Task<IActionResult> ExportPdf(int id, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        var bytes = _documentService.GeneratePdf(invoice);
        return File(bytes, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
    }

    public async Task<IActionResult> ExportExcel(int id, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        var bytes = _documentService.GenerateExcel(invoice);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{invoice.InvoiceNumber}.xlsx");
    }

    public async Task<IActionResult> CreateSales(CancellationToken cancellationToken)
    {
        var model = new InvoiceFormViewModel { InvoiceType = InvoiceType.Sales };
        await PopulateSalesSelectListsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSales(InvoiceFormViewModel model, CancellationToken cancellationToken)
    {
        model.InvoiceType = InvoiceType.Sales;
        await PopulateSalesSelectListsAsync(model, cancellationToken);
        return await SaveInvoiceAsync(model, nameof(Sales), cancellationToken);
    }

    public async Task<IActionResult> CreatePurchase(CancellationToken cancellationToken)
    {
        var model = new InvoiceFormViewModel { InvoiceType = InvoiceType.Purchase };
        await PopulatePurchaseSelectListsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePurchase(InvoiceFormViewModel model, CancellationToken cancellationToken)
    {
        model.InvoiceType = InvoiceType.Purchase;
        await PopulatePurchaseSelectListsAsync(model, cancellationToken);
        return await SaveInvoiceAsync(model, nameof(Purchase), cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, string? returnAction, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        var deactivated = await _invoiceService.DeactivateAsync(id, cancellationToken);
        if (!deactivated)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Fatura pasif duruma alındı.";
        return RedirectToAction(string.IsNullOrWhiteSpace(returnAction)
            ? (invoice.InvoiceType == InvoiceType.Sales ? nameof(Sales) : nameof(Purchase))
            : returnAction);
    }

    private async Task<IActionResult> SaveInvoiceAsync(
        InvoiceFormViewModel model,
        string redirectAction,
        CancellationToken cancellationToken)
    {
        if (model.Lines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "En az bir fatura kalemi ekleyin.");
        }

        if (model.InvoiceType == InvoiceType.Sales && !model.CustomerId.HasValue)
        {
            ModelState.AddModelError(nameof(model.CustomerId), "Satış faturası için müşteri seçilmelidir.");
        }

        if (model.InvoiceType == InvoiceType.Purchase && !model.SupplierId.HasValue)
        {
            ModelState.AddModelError(nameof(model.SupplierId), "Alış faturası için tedarikçi seçilmelidir.");
        }

        if (!ModelState.IsValid)
        {
            return View(model.InvoiceType == InvoiceType.Sales ? "CreateSales" : "CreatePurchase", model);
        }

        try
        {
            var lines = model.Lines.Select(MapLineInput).ToList();

            await _invoiceService.CreateAsync(
                model.InvoiceType,
                model.CustomerId,
                model.SupplierId,
                model.InvoiceDate,
                model.Notes,
                lines,
                cancellationToken);

            TempData["SuccessMessage"] = "Fatura başarıyla oluşturuldu.";
            return RedirectToAction(redirectAction);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model.InvoiceType == InvoiceType.Sales ? "CreateSales" : "CreatePurchase", model);
        }
    }

    private static InvoiceLineInput MapLineInput(InvoiceLineFormItem line)
    {
        var isNewProduct = line.IsNewProduct ||
                           !string.IsNullOrWhiteSpace(line.NewStockCode) ||
                           !string.IsNullOrWhiteSpace(line.NewProductName);

        if (isNewProduct)
        {
            return new InvoiceLineInput
            {
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                NewProduct = new NewPurchaseProductInput
                {
                    StockCode = line.NewStockCode?.Trim() ?? string.Empty,
                    Name = line.NewProductName?.Trim() ?? string.Empty,
                    CategoryId = line.NewCategoryId ?? 0,
                    Barcode = line.NewBarcode?.Trim()
                }
            };
        }

        return new InvoiceLineInput
        {
            StockItemId = line.StockItemId,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice
        };
    }

    private async Task PopulateSalesSelectListsAsync(InvoiceFormViewModel model, CancellationToken cancellationToken)
    {
        var customers = await _customerService.GetAllAsync(cancellationToken);
        model.Customers = customers
            .Where(c => c.IsActive)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.FullName,
                Selected = c.Id == model.CustomerId
            });

        var stocks = await _stockService.GetAllAsync(cancellationToken);
        var activeStocks = stocks.Where(s => s.IsActive).ToList();
        model.Stocks = activeStocks.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.StockCode} - {s.Name} ({s.Price:N2} ₺)"
        });
        model.StockOptions = activeStocks.Select(s => new InvoiceStockOption
        {
            Id = s.Id,
            Name = s.Name,
            StockCode = s.StockCode,
            Price = s.Price
        }).ToList();
    }

    private async Task PopulatePurchaseSelectListsAsync(InvoiceFormViewModel model, CancellationToken cancellationToken)
    {
        var suppliers = await _supplierService.GetAllAsync(cancellationToken);
        model.Suppliers = suppliers
            .Where(s => s.IsActive)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.CompanyName} (Borç: {s.Balance:N2} ₺)",
                Selected = s.Id == model.SupplierId
            });

        var stocks = await _stockService.GetAllAsync(cancellationToken);
        var activeStocks = stocks.Where(s => s.IsActive).ToList();
        model.Stocks = activeStocks.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.StockCode} - {s.Name} ({s.Price:N2} ₺)"
        });
        model.StockOptions = activeStocks.Select(s => new InvoiceStockOption
        {
            Id = s.Id,
            Name = s.Name,
            StockCode = s.StockCode,
            Price = s.Price
        }).ToList();

        var categories = await _categoryService.GetAllAsync(cancellationToken);
        var activeCategories = categories.Where(c => c.IsActive).ToList();
        model.Categories = activeCategories.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name
        });
        model.CategoryOptions = activeCategories.Select(c => new InvoiceCategoryOption
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();
    }
}

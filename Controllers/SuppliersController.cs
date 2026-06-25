using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VRLCRM.Application.Suppliers;
using VRLCRM.Domain.Constants;
using VRLCRM.Helpers;
using VRLCRM.Models.Suppliers;
using VRLCRM.Services;

namespace VRLCRM.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class SuppliersController : Controller
{
    private readonly ISupplierService _supplierService;
    private readonly AccountStatementDocumentService _statementDocumentService;

    public SuppliersController(
        ISupplierService supplierService,
        AccountStatementDocumentService statementDocumentService)
    {
        _supplierService = supplierService;
        _statementDocumentService = statementDocumentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var suppliers = await _supplierService.GetAllAsync(cancellationToken);
        return View(suppliers);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetByIdAsync(id, cancellationToken);
        if (supplier is null)
        {
            return NotFound();
        }

        return View(supplier);
    }

    public async Task<IActionResult> TransactionsPartial(int id, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetByIdAsync(id, cancellationToken);
        if (supplier is null)
        {
            return NotFound();
        }

        var orders = await _supplierService.GetOrdersAsync(id, cancellationToken);
        var purchaseInvoices = await _supplierService.GetPurchaseInvoicesAsync(id, cancellationToken);
        var salesInvoices = await _supplierService.GetSalesInvoicesAsync(id, cancellationToken);
        var payments = await _supplierService.GetPaymentsAsync(id, cancellationToken);
        var rows = AccountTransactionBuilder.FromSupplierData(orders, purchaseInvoices, salesInvoices, payments).ToList();

        ViewData["ExportPdfController"] = "Suppliers";
        ViewData["ExportPdfAction"] = "ExportPaymentsPdf";
        ViewData["PartyId"] = id;

        return PartialView("_AccountTransactionsPartial", rows);
    }

    public async Task<IActionResult> InvoicesPartial(int id, CancellationToken cancellationToken)
        => await TransactionsPartial(id, cancellationToken);

    public async Task<IActionResult> PaymentsPartial(int id, CancellationToken cancellationToken)
        => await TransactionsPartial(id, cancellationToken);

    public async Task<IActionResult> ExportPaymentsPdf(int id, bool inline = false, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierService.GetByIdAsync(id, cancellationToken);
        if (supplier is null)
        {
            return NotFound();
        }

        var orders = await _supplierService.GetOrdersAsync(id, cancellationToken);
        var purchaseInvoices = await _supplierService.GetPurchaseInvoicesAsync(id, cancellationToken);
        var salesInvoices = await _supplierService.GetSalesInvoicesAsync(id, cancellationToken);
        var payments = await _supplierService.GetPaymentsAsync(id, cancellationToken);
        var rows = AccountTransactionBuilder.FromSupplierData(orders, purchaseInvoices, salesInvoices, payments).ToList();
        var bytes = _statementDocumentService.GeneratePdf("Tedarikçi", supplier.CompanyName, rows);
        if (inline)
        {
            return PdfFileResults.AsInline(bytes);
        }

        return PdfFileResults.AsDownload(bytes, $"{supplier.CompanyName}-cari-hareketler.pdf");
    }

    public IActionResult Create()
    {
        return View(new SupplierFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _supplierService.CreateAsync(SupplierViewModelMapper.ToSupplier(model), cancellationToken);
        TempData["SuccessMessage"] = "Tedarikçi başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetByIdAsync(id, cancellationToken);
        if (supplier is null)
        {
            return NotFound();
        }

        return View(SupplierViewModelMapper.ToFormViewModel(supplier));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SupplierFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updated = await _supplierService.UpdateAsync(SupplierViewModelMapper.ToSupplier(model), cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Tedarikçi başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetByIdAsync(id, cancellationToken);
        if (supplier is null)
        {
            return NotFound();
        }

        return View(supplier);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var deleted = await _supplierService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Tedarikçi pasif duruma alındı.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var restored = await _supplierService.RestoreAsync(id, cancellationToken);
        if (!restored)
            return NotFound();

        TempData["SuccessMessage"] = "Tedarikçi tekrar aktif edildi.";
        return RedirectToAction(nameof(Details), new { id });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VRLCRM.Application.Suppliers;
using VRLCRM.Domain.Constants;
using VRLCRM.Models.Suppliers;
using VRLCRM.Services;

namespace VRLCRM.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class SuppliersController : Controller
{
    private readonly ISupplierService _supplierService;
    private readonly SupplierPaymentDocumentService _paymentDocumentService;

    public SuppliersController(
        ISupplierService supplierService,
        SupplierPaymentDocumentService paymentDocumentService)
    {
        _supplierService = supplierService;
        _paymentDocumentService = paymentDocumentService;
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

    public async Task<IActionResult> InvoicesPartial(int id, CancellationToken cancellationToken)
    {
        var invoices = await _supplierService.GetPurchaseInvoicesAsync(id, cancellationToken);
        return PartialView("_SupplierInvoicesPartial", invoices);
    }

    public async Task<IActionResult> PaymentsPartial(int id, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetByIdAsync(id, cancellationToken);
        if (supplier is null)
        {
            return NotFound();
        }

        var payments = await _supplierService.GetPaymentsAsync(id, cancellationToken);
        ViewData["SupplierId"] = id;
        return PartialView("_PaymentsPartial", payments);
    }

    public async Task<IActionResult> ExportPaymentsPdf(int id, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetByIdAsync(id, cancellationToken);
        if (supplier is null)
        {
            return NotFound();
        }

        var payments = await _supplierService.GetPaymentsAsync(id, cancellationToken);
        var bytes = _paymentDocumentService.GeneratePdf(supplier, payments);
        return File(bytes, "application/pdf", $"{supplier.CompanyName}-odemeler.pdf");
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

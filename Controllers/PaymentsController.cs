using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VRLCRM.Application.Customers;
using VRLCRM.Application.Payments;
using VRLCRM.Application.Suppliers;
using VRLCRM.Domain.Constants;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;
using VRLCRM.Models.Payments;

namespace VRLCRM.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly ICustomerService _customerService;
    private readonly ISupplierService _supplierService;

    public PaymentsController(
        IPaymentService paymentService,
        ICustomerService customerService,
        ISupplierService supplierService)
    {
        _paymentService = paymentService;
        _customerService = customerService;
        _supplierService = supplierService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var payments = await _paymentService.GetAllAsync(cancellationToken);
        return View(payments);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var payment = await _paymentService.GetByIdAsync(id, cancellationToken);
        if (payment is null) return NotFound();
        return View(payment);
    }

    public async Task<IActionResult> CreateIncoming(CancellationToken cancellationToken)
    {
        var model = new PaymentFormViewModel { Type = PaymentType.Incoming };
        await PopulateCustomersAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateIncoming(PaymentFormViewModel model, CancellationToken cancellationToken)
    {
        model.Type = PaymentType.Incoming;
        await PopulateCustomersAsync(model, cancellationToken);

        if (!model.CustomerId.HasValue)
        {
            ModelState.AddModelError(nameof(model.CustomerId), "Müşteri seçilmelidir.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _paymentService.CreateIncomingPaymentAsync(
                model.CustomerId!.Value, model.Amount, model.Method, model.PaymentDate, model.Notes, cancellationToken);
            TempData["SuccessMessage"] = "Tahsilat başarıyla kaydedildi.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> CreateCustomerOutgoing(CancellationToken cancellationToken)
    {
        var model = new PaymentFormViewModel { Type = PaymentType.Outgoing };
        await PopulateCustomersAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCustomerOutgoing(PaymentFormViewModel model, CancellationToken cancellationToken)
    {
        model.Type = PaymentType.Outgoing;
        await PopulateCustomersAsync(model, cancellationToken);

        if (!model.CustomerId.HasValue)
        {
            ModelState.AddModelError(nameof(model.CustomerId), "Müşteri seçilmelidir.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _paymentService.CreateOutgoingPaymentToCustomerAsync(
                model.CustomerId!.Value, model.Amount, model.Method, model.PaymentDate, model.Notes, cancellationToken);
            TempData["SuccessMessage"] = "Müşteriye ödeme başarıyla kaydedildi.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> CreateOutgoing(CancellationToken cancellationToken)
    {
        var model = new PaymentFormViewModel { Type = PaymentType.Outgoing };
        await PopulateSuppliersAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOutgoing(PaymentFormViewModel model, CancellationToken cancellationToken)
    {
        model.Type = PaymentType.Outgoing;
        await PopulateSuppliersAsync(model, cancellationToken);

        if (!model.SupplierId.HasValue)
        {
            ModelState.AddModelError(nameof(model.SupplierId), "Tedarikçi seçilmelidir.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _paymentService.CreateOutgoingPaymentAsync(
                model.SupplierId!.Value, model.Amount, model.Method, model.PaymentDate, model.Notes, cancellationToken);
            TempData["SuccessMessage"] = "Ödeme başarıyla kaydedildi.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> CreateSupplierIncoming(CancellationToken cancellationToken)
    {
        var model = new PaymentFormViewModel { Type = PaymentType.Incoming };
        await PopulateSuppliersAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupplierIncoming(PaymentFormViewModel model, CancellationToken cancellationToken)
    {
        model.Type = PaymentType.Incoming;
        await PopulateSuppliersAsync(model, cancellationToken);

        if (!model.SupplierId.HasValue)
        {
            ModelState.AddModelError(nameof(model.SupplierId), "Tedarikçi seçilmelidir.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _paymentService.CreateIncomingPaymentFromSupplierAsync(
                model.SupplierId!.Value, model.Amount, model.Method, model.PaymentDate, model.Notes, cancellationToken);
            TempData["SuccessMessage"] = "Tedarikçiden tahsilat başarıyla kaydedildi.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _paymentService.DeactivateAsync(id, cancellationToken);
            if (!success)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Fiş iptal edildi ve bakiyeler güncellendi.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCustomersAsync(PaymentFormViewModel model, CancellationToken cancellationToken)
    {
        var customers = await _customerService.GetAllAsync(cancellationToken);
        model.Customers = customers
            .Where(c => c.IsActive)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.FullName} (Mevcut Borç: {c.Balance:N2} ₺)",
                Selected = c.Id == model.CustomerId
            });
    }

    private async Task PopulateSuppliersAsync(PaymentFormViewModel model, CancellationToken cancellationToken)
    {
        var suppliers = await _supplierService.GetAllAsync(cancellationToken);
        model.Suppliers = suppliers
            .Where(s => s.IsActive)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.CompanyName} (Mevcut Borcumuz: {s.Balance:N2} ₺)",
                Selected = s.Id == model.SupplierId
            });
    }

}

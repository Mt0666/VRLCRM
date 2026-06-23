using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Customers;
using VRLCRM.Domain.Constants;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;
using VRLCRM.Models.Customers;
using VRLCRM.Services;

namespace VRLCRM.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CustomerPaymentDocumentService _paymentDocumentService;

    public CustomersController(
        ICustomerService customerService,
        UserManager<ApplicationUser> userManager,
        CustomerPaymentDocumentService paymentDocumentService)
    {
        _customerService = customerService;
        _userManager = userManager;
        _paymentDocumentService = paymentDocumentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var customers = await _customerService.GetAllAsync(cancellationToken);
        return View(customers);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        return View(customer);
    }

    public async Task<IActionResult> OrdersPartial(int id, CancellationToken cancellationToken)
    {
        var orders = await _customerService.GetOrdersAsync(id, cancellationToken);
        var invoices = await _customerService.GetSalesInvoicesAsync(id, cancellationToken);

        // Siparişlere bağlı fatura ID'leri — direkt faturalar bunların dışında kalanlar
        var linkedInvoiceIds = orders
            .Where(o => o.SalesInvoiceId.HasValue)
            .Select(o => o.SalesInvoiceId!.Value)
            .ToHashSet();

        var rows = new List<CustomerTransactionRow>();

        foreach (var o in orders)
        {
            rows.Add(new CustomerTransactionRow
            {
                Id = o.Id,
                Number = o.OrderNumber,
                Date = o.OrderDate,
                IsDirectInvoice = false,
                StatusLabel = o.StatusLabel,
                StatusColor = o.Status switch
                {
                    OrderStatus.Approved => "success",
                    OrderStatus.Cancelled => "danger",
                    _ => "secondary"
                },
                TotalAmount = o.TotalAmount
            });
        }

        foreach (var inv in invoices.Where(i => !linkedInvoiceIds.Contains(i.Id) && i.IsActive))
        {
            rows.Add(new CustomerTransactionRow
            {
                Id = inv.Id,
                Number = inv.InvoiceNumber,
                Date = inv.InvoiceDate,
                IsDirectInvoice = true,
                StatusLabel = "Faturalandırıldı",
                StatusColor = "warning",
                TotalAmount = inv.TotalAmount
            });
        }

        rows = [.. rows.OrderByDescending(r => r.Date)];
        return PartialView("_OrdersPartial", rows);
    }

    public async Task<IActionResult> InvoicesPartial(int id, CancellationToken cancellationToken)
    {
        var invoices = await _customerService.GetSalesInvoicesAsync(id, cancellationToken);
        return PartialView("_InvoicesPartial", invoices);
    }

    public async Task<IActionResult> PaymentsPartial(int id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        var payments = await _customerService.GetPaymentsAsync(id, cancellationToken);
        ViewData["CustomerId"] = id;
        return PartialView("_PaymentsPartial", payments);
    }

    public async Task<IActionResult> ExportPaymentsPdf(int id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        var payments = await _customerService.GetPaymentsAsync(id, cancellationToken);
        var bytes = _paymentDocumentService.GeneratePdf(customer, payments);
        return File(bytes, "application/pdf", $"{customer.FullName}-odemeler.pdf");
    }

    public IActionResult Create()
    {
        return View(new CustomerFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _customerService.CreateAsync(
                CustomerViewModelMapper.ToCustomer(model),
                CustomerViewModelMapper.ToAddress(model),
                model.B2bLoginPhone,
                model.Password,
                cancellationToken);

            TempData["SuccessMessage"] = "Müşteri başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer is null)
            return NotFound();

        var linkedUser = await _userManager.Users.FirstOrDefaultAsync(u => u.CustomerId == id, cancellationToken);
        var vm = CustomerViewModelMapper.ToFormViewModel(customer);
        vm.B2bLoginPhone = linkedUser?.PhoneNumber;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var updated = await _customerService.UpdateAsync(
                CustomerViewModelMapper.ToCustomer(model),
                CustomerViewModelMapper.ToAddress(model),
                model.B2bLoginPhone,
                model.Password,
                cancellationToken);

            if (!updated)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Müşteri başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        return View(customer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var deleted = await _customerService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound();

        TempData["SuccessMessage"] = "Müşteri pasif duruma alındı.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var restored = await _customerService.RestoreAsync(id, cancellationToken);
        if (!restored)
            return NotFound();

        TempData["SuccessMessage"] = "Müşteri tekrar aktif edildi.";
        return RedirectToAction(nameof(Details), new { id });
    }
}

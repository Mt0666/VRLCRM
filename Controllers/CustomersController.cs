using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Customers;
using VRLCRM.Domain.Constants;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;
using VRLCRM.Helpers;
using VRLCRM.Models.Customers;
using VRLCRM.Services;

namespace VRLCRM.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AccountStatementDocumentService _statementDocumentService;

    public CustomersController(
        ICustomerService customerService,
        UserManager<ApplicationUser> userManager,
        AccountStatementDocumentService statementDocumentService)
    {
        _customerService = customerService;
        _userManager = userManager;
        _statementDocumentService = statementDocumentService;
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

    public async Task<IActionResult> TransactionsPartial(int id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        var orders = await _customerService.GetOrdersAsync(id, cancellationToken);
        var invoices = await _customerService.GetSalesInvoicesAsync(id, cancellationToken);
        var payments = await _customerService.GetPaymentsAsync(id, cancellationToken);
        var rows = AccountTransactionBuilder.FromCustomerData(orders, invoices, payments).ToList();

        ViewData["ExportPdfController"] = "Customers";
        ViewData["ExportPdfAction"] = "ExportPaymentsPdf";
        ViewData["PartyId"] = id;

        return PartialView("_AccountTransactionsPartial", rows);
    }

    public async Task<IActionResult> OrdersPartial(int id, CancellationToken cancellationToken)
        => await TransactionsPartial(id, cancellationToken);

    public async Task<IActionResult> InvoicesPartial(int id, CancellationToken cancellationToken)
    {
        var invoices = await _customerService.GetSalesInvoicesAsync(id, cancellationToken);
        return PartialView("_InvoicesPartial", invoices);
    }

    public async Task<IActionResult> PaymentsPartial(int id, CancellationToken cancellationToken)
        => await TransactionsPartial(id, cancellationToken);

    public async Task<IActionResult> ExportPaymentsPdf(int id, bool inline = false, CancellationToken cancellationToken = default)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        var orders = await _customerService.GetOrdersAsync(id, cancellationToken);
        var invoices = await _customerService.GetSalesInvoicesAsync(id, cancellationToken);
        var payments = await _customerService.GetPaymentsAsync(id, cancellationToken);
        var rows = AccountTransactionBuilder.FromCustomerData(orders, invoices, payments).ToList();
        var bytes = _statementDocumentService.GeneratePdf("Müşteri", customer.FullName, rows);
        if (inline)
        {
            return PdfFileResults.AsInline(bytes);
        }

        return PdfFileResults.AsDownload(bytes, $"{customer.FullName}-cari-hareketler.pdf");
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

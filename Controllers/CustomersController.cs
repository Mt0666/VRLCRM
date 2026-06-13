using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Customers;
using VRLCRM.Domain.Constants;
using VRLCRM.Domain.Entities;
using VRLCRM.Models.Customers;

namespace VRLCRM.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersController(ICustomerService customerService, UserManager<ApplicationUser> userManager)
    {
        _customerService = customerService;
        _userManager = userManager;
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
        return PartialView("_OrdersPartial", orders);
    }

    public async Task<IActionResult> InvoicesPartial(int id, CancellationToken cancellationToken)
    {
        var invoices = await _customerService.GetSalesInvoicesAsync(id, cancellationToken);
        return PartialView("_InvoicesPartial", invoices);
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
                model.Email,
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
        vm.Email = linkedUser?.Email;
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
                model.Email,
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

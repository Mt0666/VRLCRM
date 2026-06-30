using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Common;
using VRLCRM.Application.Customers;
using VRLCRM.Domain.Entities;
using VRLCRM.Infrastructure.Data;
using VRLCRM.Models.Auth;

namespace VRLCRM.Controllers;

public class ShopAuthController : Controller
{
    private const string CustomerRole = "Customer";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public ShopAuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
        {
            if (User.IsInRole(CustomerRole))
                return RedirectToAction("Index", "Shop");

            return RedirectToAction("Index", "Dashboards");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new ShopLoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(ShopLoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var normalizedPhone = PhoneNormalizer.Normalize(model.Phone);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            ModelState.AddModelError(nameof(model.Phone), "Geçerli bir telefon numarası girin.");
            return View(model);
        }

        var user = await FindB2bUserAsync(normalizedPhone);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Bu telefon için B2B hesabı bulunamadı.");
            return View(model);
        }

        if (!await _userManager.IsInRoleAsync(user, CustomerRole))
        {
            await _userManager.AddToRoleAsync(user, CustomerRole);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Shop");
        }

        ModelState.AddModelError(string.Empty, "Telefon veya şifre hatalı.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = CustomerRole)]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    private async Task<ApplicationUser?> FindB2bUserAsync(string normalizedPhone)
    {
        var user = await _userManager.FindByNameAsync(normalizedPhone);
        if (user?.CustomerId != null)
        {
            return user;
        }

        var b2bUsers = await _userManager.Users
            .Where(u => u.CustomerId != null)
            .ToListAsync();

        user = b2bUsers.FirstOrDefault(u =>
            PhoneNormalizer.Normalize(u.UserName) == normalizedPhone ||
            PhoneNormalizer.Normalize(u.PhoneNumber) == normalizedPhone);

        if (user is not null)
        {
            return user;
        }

        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new { c.Id, c.PhoneNumber })
            .ToListAsync();

        var customerId = customers
            .FirstOrDefault(c => PhoneNormalizer.Normalize(c.PhoneNumber) == normalizedPhone)
            ?.Id;

        if (customerId is null)
        {
            return null;
        }

        return b2bUsers.FirstOrDefault(u => u.CustomerId == customerId);
    }
}

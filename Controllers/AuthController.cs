using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VRLCRM.Domain.Constants;
using VRLCRM.Domain.Entities;
using VRLCRM.Models.Auth;

namespace VRLCRM.Controllers;

[Authorize]
public class AuthController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult LoginBasic(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
        {
            if (User.IsInRole(AppRoles.Customer))
                return RedirectToAction("Index", "Shop");
            if (User.IsInRole(AppRoles.Personel))
                return RedirectToAction("Index", "Orders");
            return RedirectToAction("Index", "Dashboards");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginBasic(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null && await _userManager.IsInRoleAsync(user, "Customer"))
        {
            ModelState.AddModelError(string.Empty, "Müşteri hesabıyla yönetim paneline giriş yapılamaz. B2B Mağaza girişini kullanınız.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            if (User.IsInRole(AppRoles.Personel))
                return RedirectToAction("Index", "Orders");

            return RedirectToAction("Index", "Dashboards");
        }

        ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("LoginBasic", "Auth");
    }

    [AllowAnonymous]
    public IActionResult ForgotPasswordBasic() => View();

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

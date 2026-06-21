using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VRLCRM.Application.Users;
using VRLCRM.Domain.Constants;
using VRLCRM.Domain.Entities;
using VRLCRM.Models.Auth;

namespace VRLCRM.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllUsersAsync(cancellationToken);
        var userViewModels = new List<UserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userService.GetRolesAsync(user, cancellationToken);
            var role = roles.FirstOrDefault() ?? "";

            if (role == AppRoles.Customer)
                continue;

            userViewModels.Add(new UserViewModel
            {
                Id = user.Id,
                PhoneNumber = user.PhoneNumber ?? user.UserName ?? "",
                FullName = user.FullName ?? "",
                Role = role
            });
        }

        return View(userViewModels);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new UserFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = new ApplicationUser
            {
                UserName = model.PhoneNumber,
                Email = model.PhoneNumber + "@staff.local",
                PhoneNumber = model.PhoneNumber,
                FullName = model.FullName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            await _userService.CreateUserAsync(user, model.Password!, model.SelectedRole, cancellationToken);
            TempData["SuccessMessage"] = "Kullanıcı başarıyla oluşturuldu.";
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
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _userService.DeleteUserAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Kullanıcı başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}

public class UserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

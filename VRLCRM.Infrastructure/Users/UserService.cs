using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Users;
using VRLCRM.Domain.Entities;

namespace VRLCRM.Infrastructure.Users;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<ApplicationUser>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _userManager.Users.ToListAsync(cancellationToken);
    }

    public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<bool> CreateUserAsync(ApplicationUser user, string password, string role, CancellationToken cancellationToken = default)
    {
        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, role);
            return true;
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        throw new InvalidOperationException(errors);
    }

    public async Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        if (user.Email == "admin@vrlcrm.local")
        {
            throw new InvalidOperationException("Sistem yöneticisi silinemez.");
        }

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }
}

using Microsoft.AspNetCore.Identity;
using VRLCRM.Domain.Entities;

namespace VRLCRM.Infrastructure.Identity;

/// <summary>
/// B2B hesaplarında e-posta alanı telefon numarası olarak tutulur.
/// </summary>
public class B2bFriendlyUserValidator : IUserValidator<ApplicationUser>
{
    private readonly UserValidator<ApplicationUser> _defaultValidator = new();

    public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        if (user.CustomerId != null)
        {
            return IdentityResult.Success;
        }

        return await _defaultValidator.ValidateAsync(manager, user);
    }
}

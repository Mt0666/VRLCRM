using Microsoft.AspNetCore.Identity;

namespace VRLCRM.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}

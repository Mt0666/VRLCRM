using Microsoft.AspNetCore.Identity;

namespace VRLCRM.Domain.Entities;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
}

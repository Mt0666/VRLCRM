namespace VRLCRM.Infrastructure.Options;

public class SeedDataOptions
{
    public const string SectionName = "SeedData";

    public string AdminEmail { get; set; } = "admin@vrlcrm.local";

    public string AdminPassword { get; set; } = string.Empty;

    public string[] DefaultRoles { get; set; } = ["Admin", "Personel"];
}

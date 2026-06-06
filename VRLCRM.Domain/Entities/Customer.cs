using VRLCRM.Domain.Common;

namespace VRLCRM.Domain.Entities;

public class Customer : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? CompanyName { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public Address? Address { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}

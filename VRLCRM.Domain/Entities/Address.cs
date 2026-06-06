using VRLCRM.Domain.Common;

namespace VRLCRM.Domain.Entities;

public class Address : BaseEntity
{
    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public string City { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string AddressLine { get; set; } = string.Empty;
}

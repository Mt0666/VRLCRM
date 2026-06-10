using VRLCRM.Domain.Common;

namespace VRLCRM.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<StockItem> StockItems { get; set; } = [];
}

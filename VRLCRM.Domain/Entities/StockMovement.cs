using VRLCRM.Domain.Common;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Domain.Entities;

public class StockMovement : BaseEntity
{
    public int StockItemId { get; set; }

    public StockItem StockItem { get; set; } = null!;

    public StockMovementType MovementType { get; set; }

    public int Quantity { get; set; }

    public StockMovementReferenceType ReferenceType { get; set; }

    public int ReferenceId { get; set; }

    public DateTime MovementDate { get; set; }

    public string? Notes { get; set; }
}

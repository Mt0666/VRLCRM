namespace VRLCRM.Application.Stocks;

/// <summary>Bir ürünün alış ve satış hareket geçmişi.</summary>
public class StockMovementHistory
{
    public IReadOnlyList<StockMovementRow> Purchases { get; init; } = [];

    public IReadOnlyList<StockMovementRow> Sales { get; init; } = [];
}

/// <summary>Tek bir alış/satış fatura satırının özeti.</summary>
public class StockMovementRow
{
    public DateTime Date { get; init; }

    public string DocumentNumber { get; init; } = string.Empty;

    /// <summary>Alışta tedarikçi, satışta müşteri adı.</summary>
    public string PartyName { get; init; } = "-";

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal { get; init; }
}

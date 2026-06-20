namespace VRLCRM.Models.Invoices;

public class PurchaseImportLineResult
{
    public bool IsNew { get; set; }

    public string? StockId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string? Barcode { get; set; }

    public int CriticalStockLevel { get; set; } = 5;

    public int Qty { get; set; }

    public decimal Price { get; set; }

    public decimal VatRate { get; set; }
}

public class PurchaseImportResult
{
    public List<PurchaseImportLineResult> Lines { get; set; } = [];

    public List<string> Warnings { get; set; } = [];

    public List<string> Errors { get; set; } = [];

    public bool Success => Errors.Count == 0;
}

using VRLCRM.Domain.Entities;

namespace VRLCRM.Models.Stocks;

public static class StockViewModelMapper
{
    public static StockFormViewModel ToFormViewModel(StockItem stockItem)
    {
        return new StockFormViewModel
        {
            Id = stockItem.Id,
            StockCode = stockItem.StockCode,
            CategoryId = stockItem.CategoryId,
            ImageUrl = stockItem.ImageUrl,
            Name = stockItem.Name,
            PurchasePrice = stockItem.PurchasePrice,
            Price = stockItem.Price,
            VatRate = stockItem.VatRate,
            StockQuantity = stockItem.StockQuantity,
            CriticalStockLevel = stockItem.CriticalStockLevel,
            Barcode = stockItem.Barcode,
            Description = stockItem.Description
        };
    }

    public static StockItem ToStockItem(StockFormViewModel model)
    {
        return new StockItem
        {
            Id = model.Id,
            StockCode = model.StockCode.Trim(),
            CategoryId = model.CategoryId,
            ImageUrl = model.ImageUrl,
            Name = model.Name.Trim(),
            PurchasePrice = model.PurchasePrice ?? 0m,
            Price = model.Price ?? 0m,
            VatRate = model.VatRate,
            StockQuantity = model.StockQuantity,
            CriticalStockLevel = model.CriticalStockLevel,
            Barcode = model.Barcode?.Trim(),
            Description = model.Description?.Trim()
        };
    }
}

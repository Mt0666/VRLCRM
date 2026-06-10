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
            Price = stockItem.Price,
            StockQuantity = stockItem.StockQuantity,
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
            Price = model.Price,
            StockQuantity = model.StockQuantity,
            Barcode = model.Barcode?.Trim(),
            Description = model.Description?.Trim()
        };
    }
}

using ClosedXML.Excel;
using System.Globalization;
using VRLCRM.Application.Categories;
using VRLCRM.Application.Stocks;
using VRLCRM.Domain.Entities;
using VRLCRM.Models.Invoices;

namespace VRLCRM.Services;

public class PurchaseInvoiceImportService
{
    private const string BrandColor = "#696CFF";
    private const int HeaderRow = 3;
    private const int FirstDataRow = 4;

    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly StringComparer TurkishNameComparer =
        StringComparer.Create(TurkishCulture, true);

    private static readonly string[] Headers =
    [
        "Stok Kodu *",
        "Ürün Adı",
        "Barkod",
        "Kategori",
        "Adet *",
        "Alış Fiyatı *",
        "KDV %",
        "Kritik Stok"
    ];

    private readonly IStockService _stockService;
    private readonly ICategoryService _categoryService;

    public PurchaseInvoiceImportService(IStockService stockService, ICategoryService categoryService)
    {
        _stockService = stockService;
        _categoryService = categoryService;
    }

    public async Task<byte[]> GenerateTemplateAsync(CancellationToken cancellationToken = default)
    {
        var categories = (await _categoryService.GetAllAsync(cancellationToken))
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToList();

        using var workbook = new XLWorkbook();

        var sheet = workbook.Worksheets.Add("Alış Kalemleri");
        sheet.Column(1).Width = 16;
        sheet.Column(2).Width = 28;
        sheet.Column(3).Width = 16;
        sheet.Column(4).Width = 18;
        sheet.Column(5).Width = 10;
        sheet.Column(6).Width = 14;
        sheet.Column(7).Width = 10;
        sheet.Column(8).Width = 12;

        sheet.Range("A1:H1").Merge().Value =
            "Alış Faturası Excel Şablonu — Mevcut ürünler için Stok Kodu + Adet + Alış Fiyatı yeterlidir. " +
            "Stokta tanımsız ürünler için Ürün Adı ve Kategori zorunludur. Kategori yoksa Excel yüklenirken otomatik oluşturulur, sonra ürünler tabloya eklenir. " +
            "ÖRNEK satırları silin veya üzerine yazın.";
        sheet.Range("A1:H1").Style.Alignment.SetWrapText(true);
        sheet.Row(1).Height = 42;

        sheet.Range("A2:H2").Merge().Value =
            "* zorunlu alanlar | Mevcut ürün: Stok Kodu eşleşirse yeterli | Yeni ürün: Stok Kodu + Ürün Adı + Kategori + Adet + Alış Fiyatı | Yeni kategori Excel yüklenirken oluşturulur";
        sheet.Range("A2:H2").Style.Font.SetItalic(true).Font.SetFontColor(XLColor.Gray);

        for (var col = 0; col < Headers.Length; col++)
        {
            sheet.Cell(HeaderRow, col + 1).Value = Headers[col];
        }

        sheet.Range(HeaderRow, 1, HeaderRow, Headers.Length).Style.Font.SetBold()
            .Fill.SetBackgroundColor(XLColor.FromHtml(BrandColor))
            .Font.SetFontColor(XLColor.White);

        var firstCategory = categories.FirstOrDefault()?.Name ?? "Genel";
        sheet.Cell(FirstDataRow, 1).Value = "ORNEK-MEVCUT";
        sheet.Cell(FirstDataRow, 2).Value = "Sistemdeki stok kodunu yazın";
        sheet.Cell(FirstDataRow, 5).Value = 10;
        sheet.Cell(FirstDataRow, 6).Value = 50m;
        sheet.Cell(FirstDataRow, 7).Value = 0.20m;
        sheet.Cell(FirstDataRow, 7).Style.NumberFormat.SetFormat("0%");

        sheet.Cell(FirstDataRow + 1, 1).Value = "ORNEK-YENI-001";
        sheet.Cell(FirstDataRow + 1, 2).Value = "Yeni Ürün Örneği";
        sheet.Cell(FirstDataRow + 1, 4).Value = firstCategory;
        sheet.Cell(FirstDataRow + 1, 5).Value = 5;
        sheet.Cell(FirstDataRow + 1, 6).Value = 25m;
        sheet.Cell(FirstDataRow + 1, 7).Value = 0.20m;
        sheet.Cell(FirstDataRow + 1, 7).Style.NumberFormat.SetFormat("0%");
        sheet.Cell(FirstDataRow + 1, 8).Value = 5;

        sheet.Range(FirstDataRow, 1, FirstDataRow + 1, Headers.Length)
            .Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FFF8E1"));

        sheet.SheetView.FreezeRows(HeaderRow);

        var categorySheet = workbook.Worksheets.Add("Kategoriler");
        categorySheet.Cell(1, 1).Value = "Kategori Adı";
        categorySheet.Cell(1, 1).Style.Font.SetBold()
            .Fill.SetBackgroundColor(XLColor.FromHtml(BrandColor))
            .Font.SetFontColor(XLColor.White);
        categorySheet.Column(1).Width = 30;

        var row = 2;
        foreach (var category in categories)
        {
            categorySheet.Cell(row, 1).Value = category.Name;
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<PurchaseImportResult> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var result = new PurchaseImportResult();
        var stocks = (await _stockService.GetAllAsync(cancellationToken))
            .Where(s => s.IsActive)
            .ToList();
        var categories = (await _categoryService.GetAllAsync(cancellationToken))
            .Where(c => c.IsActive)
            .ToList();

        var stockByCode = stocks.ToDictionary(s => s.StockCode.Trim().ToUpperInvariant(), s => s);
        var stockByBarcode = stocks
            .Where(s => !string.IsNullOrWhiteSpace(s.Barcode))
            .GroupBy(s => s.Barcode!.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First());
        var categoryByName = categories.ToDictionary(
            c => NormalizeCategoryKey(c.Name),
            c => c,
            TurkishNameComparer);

        var createdCategoryKeys = new HashSet<string>(TurkishNameComparer);

        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Contains("Alış", StringComparison.OrdinalIgnoreCase) ||
            w.Name.Contains("Alis", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheet(1);

        var headerRow = FindHeaderRow(sheet) ?? HeaderRow;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        var merged = new Dictionary<string, PurchaseImportLineResult>(StringComparer.OrdinalIgnoreCase);

        for (var row = headerRow + 1; row <= lastRow; row++)
        {
            var stockCode = GetCellText(sheet, row, 1);
            var productName = GetCellText(sheet, row, 2);
            var barcode = GetCellText(sheet, row, 3);
            var categoryName = GetCellText(sheet, row, 4);
            var qtyText      = GetCellText(sheet, row, 5);
            var priceText    = GetCellText(sheet, row, 6);
            var vatText      = GetCellText(sheet, row, 7);
            var criticalText = GetCellText(sheet, row, 8);

            if (string.IsNullOrWhiteSpace(stockCode) && string.IsNullOrWhiteSpace(productName))
            {
                continue;
            }

            if (IsExampleRow(stockCode))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(stockCode))
            {
                result.Errors.Add($"Satır {row}: Stok kodu zorunludur.");
                continue;
            }

            if (!TryParseInt(qtyText, out var qty) || qty <= 0)
            {
                result.Errors.Add($"Satır {row}: Geçerli bir adet girin.");
                continue;
            }

            if (!TryGetCellDecimal(sheet, row, 6, out var price) || price < 0)
            {
                result.Errors.Add($"Satır {row}: Geçerli bir alış fiyatı girin.");
                continue;
            }

            var vatRate = TryParseVatRate(sheet, row, 7, vatText, out var parsedVat)
                ? parsedVat
                : 20m;
            var criticalLevel = TryParseInt(criticalText, out var parsedCritical) && parsedCritical >= 0
                ? parsedCritical
                : 5;

            if (stockByCode.TryGetValue(stockCode.ToUpperInvariant(), out var existingStock))
            {
                MergeLine(merged, new PurchaseImportLineResult
                {
                    IsNew = false,
                    StockId = existingStock.Id.ToString(),
                    Name = existingStock.Name,
                    Code = existingStock.StockCode,
                    Qty = qty,
                    Price = price,
                    VatRate = vatText.Length > 0 ? vatRate : existingStock.VatRate
                }, $"stock:{existingStock.Id}");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(barcode) &&
                stockByBarcode.TryGetValue(barcode.ToUpperInvariant(), out var stockByBar))
            {
                MergeLine(merged, new PurchaseImportLineResult
                {
                    IsNew = false,
                    StockId = stockByBar.Id.ToString(),
                    Name = stockByBar.Name,
                    Code = stockByBar.StockCode,
                    Qty = qty,
                    Price = price,
                    VatRate = vatText.Length > 0 ? vatRate : stockByBar.VatRate
                }, $"stock:{stockByBar.Id}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(productName))
            {
                result.Errors.Add($"Satır {row}: \"{stockCode}\" stokta bulunamadı. Yeni ürün için Ürün Adı girin.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                result.Errors.Add($"Satır {row}: \"{stockCode}\" yeni ürün — Kategori zorunludur.");
                continue;
            }

            var category = await ResolveImportCategoryAsync(
                categoryName,
                categoryByName,
                createdCategoryKeys,
                result,
                cancellationToken);

            MergeLine(merged, new PurchaseImportLineResult
            {
                IsNew = true,
                Name = productName,
                Code = stockCode,
                CategoryId = category.Id,
                Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode,
                CriticalStockLevel = criticalLevel,
                Qty = qty,
                Price = price,
                VatRate = vatRate
            }, $"new:{stockCode.ToUpperInvariant()}");
        }

        if (merged.Count == 0 && result.Errors.Count == 0)
        {
            result.Errors.Add("Excel dosyasında işlenecek satır bulunamadı.");
        }

        result.Lines = merged.Values.ToList();
        return result;
    }

    private async Task<Category> ResolveImportCategoryAsync(
        string categoryName,
        Dictionary<string, Category> categoryByName,
        HashSet<string> createdCategoryKeys,
        PurchaseImportResult result,
        CancellationToken cancellationToken)
    {
        var trimmed = categoryName.Trim();
        var key = NormalizeCategoryKey(trimmed);
        if (categoryByName.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var (category, created) = await _categoryService.GetOrCreateByNameAsync(trimmed, cancellationToken);
        categoryByName[key] = category;

        if (created && createdCategoryKeys.Add(key))
        {
            result.Warnings.Add($"\"{category.Name}\" kategorisi oluşturuldu.");
        }

        return category;
    }

    private static string NormalizeCategoryKey(string name) =>
        name.Trim().ToUpper(TurkishCulture);

    private static int? FindHeaderRow(IXLWorksheet sheet)
    {
        var lastRow = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 10, 20);
        for (var row = 1; row <= lastRow; row++)
        {
            var first = GetCellText(sheet, row, 1);
            var fifth = GetCellText(sheet, row, 5);
            if (first.Contains("Stok Kodu", StringComparison.OrdinalIgnoreCase) &&
                fifth.Contains("Adet", StringComparison.OrdinalIgnoreCase))
            {
                return row;
            }
        }

        return null;
    }

    private static bool IsExampleRow(string stockCode) =>
        stockCode.StartsWith("ORNEK", StringComparison.OrdinalIgnoreCase) ||
        stockCode.StartsWith("ÖRNEK", StringComparison.OrdinalIgnoreCase);

    private static void MergeLine(
        Dictionary<string, PurchaseImportLineResult> merged,
        PurchaseImportLineResult line,
        string key)
    {
        if (merged.TryGetValue(key, out var existing))
        {
            existing.Qty += line.Qty;
            existing.Price = line.Price;
            existing.VatRate = line.VatRate;
            if (line.IsNew)
            {
                existing.Name = line.Name;
                existing.Barcode = line.Barcode;
                existing.CategoryId = line.CategoryId;
                existing.CategoryName = line.CategoryName;
                existing.CriticalStockLevel = line.CriticalStockLevel;
            }
            return;
        }

        merged[key] = line;
    }

    private static string GetCellText(IXLWorksheet sheet, int row, int col)
    {
        var cell = sheet.Cell(row, col);
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        return cell.GetFormattedString().Trim();
    }

    /// <summary>
    /// Sayısal hücre ise doğrudan numeric değer döner (locale sorununu önler).
    /// Metin hücresi ise string parse edilir.
    /// </summary>
    private static bool TryGetCellDecimal(IXLWorksheet sheet, int row, int col, out decimal value)
    {
        value = 0;
        var cell = sheet.Cell(row, col);
        if (cell.IsEmpty()) return false;

        if (cell.DataType == XLDataType.Number)
        {
            value = (decimal)cell.GetDouble();
            return true;
        }

        var text = cell.GetFormattedString().Replace("₺", "").Trim();
        return TryParseDecimal(text, out value);
    }

    private static bool TryParseInt(string text, out int value)
    {
        text = text.Replace(".", "").Replace(",", "").Trim();
        return int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out value)
            || int.TryParse(text, out value);
    }

    /// <summary>
    /// Ondalık ayracı "." veya "," olarak destekler.
    /// Son gelen ayraç ondalık ayracı olarak kabul edilir:
    ///   "10.75"    → 10,75  (nokta = ondalık)
    ///   "10,75"    → 10,75  (virgül = ondalık)
    ///   "1.000,75" → 1000,75 (Türkçe format)
    ///   "1,000.75" → 1000,75 (İngilizce format)
    /// </summary>
    private static bool TryParseDecimal(string text, out decimal value)
    {
        value = 0;
        text = text.Replace("₺", "").Replace(" ", "").Trim();
        if (string.IsNullOrEmpty(text)) return false;

        var dotIdx   = text.LastIndexOf('.');
        var commaIdx = text.LastIndexOf(',');

        string normalized;
        if (dotIdx > commaIdx)
        {
            // Nokta ondalık ayraç: "10.75" veya "1,000.75"
            normalized = text.Replace(",", "");
        }
        else if (commaIdx > dotIdx)
        {
            // Virgül ondalık ayraç: "10,75" veya "1.000,75"
            normalized = text.Replace(".", "").Replace(",", ".");
        }
        else
        {
            // Ayraç yok
            normalized = text;
        }

        return decimal.TryParse(normalized, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseVatRate(IXLWorksheet sheet, int row, int col, string text, out decimal vatRate)
    {
        vatRate = 20m;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var cell = sheet.Cell(row, col);
        if (cell.DataType == XLDataType.Number && cell.GetDouble() <= 1)
        {
            vatRate = Math.Round((decimal)cell.GetDouble() * 100m, 2);
            return true;
        }

        text = text.Replace("%", "").Trim();
        if (TryParseDecimal(text, out vatRate))
        {
            return true;
        }

        vatRate = 20m;
        return false;
    }
}

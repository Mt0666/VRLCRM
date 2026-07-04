using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VRLCRM.Application.Categories;
using VRLCRM.Application.Stocks;
using VRLCRM.Domain.Constants;
using VRLCRM.Helpers;
using VRLCRM.Models.Stocks;
using VRLCRM.Services;

namespace VRLCRM.Controllers;

[Authorize(Roles = AppRoles.AdminAndPersonel)]
public class StocksController : Controller
{
    private readonly IStockService _stockService;
    private readonly ICategoryService _categoryService;
    private readonly IStockImageStorage _imageStorage;
    private readonly StockDocumentService _documentService;

    public StocksController(
        IStockService stockService,
        ICategoryService categoryService,
        IStockImageStorage imageStorage,
        StockDocumentService documentService)
    {
        _stockService = stockService;
        _categoryService = categoryService;
        _imageStorage = imageStorage;
        _documentService = documentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var stocks = await _stockService.GetAllAsync(cancellationToken);
        return View(stocks);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> OptimizeImages(
        [FromServices] StockImageOptimizer optimizer,
        CancellationToken cancellationToken)
    {
        var summary = await optimizer.RunAsync(cancellationToken);
        TempData["SuccessMessage"] =
            $"Görsel optimizasyonu tamamlandı: {summary.Optimized} küçültüldü, " +
            $"{summary.Skipped} atlandı, {summary.Failed} hata (işlenen {summary.Total}).";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ExportExcel(CancellationToken cancellationToken)
    {
        var stocks = await _stockService.GetAllAsync(cancellationToken);
        var bytes = _documentService.GenerateExcel(stocks);
        var fileName = $"urunler-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    public async Task<IActionResult> ExportPdf(bool inline = false, CancellationToken cancellationToken = default)
    {
        var stocks = await _stockService.GetAllAsync(cancellationToken);
        var bytes = _documentService.GeneratePdf(stocks);
        if (inline)
        {
            return PdfFileResults.AsInline(bytes);
        }

        var fileName = $"urunler-{DateTime.Now:yyyyMMdd-HHmm}.pdf";
        return PdfFileResults.AsDownload(bytes, fileName);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var stock = await _stockService.GetByIdAsync(id, cancellationToken);
        if (stock is null)
        {
            return NotFound();
        }

        ViewData["History"] = await _stockService.GetMovementHistoryAsync(id, cancellationToken);
        return View(stock);
    }

    public async Task<IActionResult> Label(int id, CancellationToken cancellationToken)
    {
        var stock = await _stockService.GetByIdAsync(id, cancellationToken);
        if (stock is null)
        {
            return NotFound();
        }

        // Barkod yoksa stok kodunu barkoda çevir (her ürün taranabilir olsun).
        var barcodeText = string.IsNullOrWhiteSpace(stock.Barcode) ? stock.StockCode : stock.Barcode;
        var barcodePng = BarcodeGenerator.ToCode128Png(barcodeText);
        var pdf = StockLabelDocument.Build(stock.Name, stock.StockCode, barcodeText, barcodePng, stock.Price);

        // Tam 4×6 inç PDF; tarayıcının PDF görüntüleyicisinde inline açılır (yeni sekme) → oradan yazdırılır.
        return File(pdf, "application/pdf");
    }

    /// <summary>Ürünün PPLA etiket komutlarını döner; tarayıcı bunu yerel yazıcı köprüsüne iletir.</summary>
    public async Task<IActionResult> LabelPpla(int id, CancellationToken cancellationToken)
    {
        var stock = await _stockService.GetByIdAsync(id, cancellationToken);
        if (stock is null)
        {
            return NotFound();
        }

        var barcodeText = string.IsNullOrWhiteSpace(stock.Barcode) ? stock.StockCode : stock.Barcode;
        var ppla = PplaLabelBuilder.Build(stock.Name, stock.StockCode, barcodeText, stock.Price);
        return Content(ppla, "text/plain; charset=utf-8");
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new StockFormViewModel();
        await PopulateCategoriesAsync(model, cancellationToken);
        ViewBag.EnableBarcodeScanner = true;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockFormViewModel model, CancellationToken cancellationToken)
    {
        await PopulateCategoriesAsync(model, cancellationToken);

        if (await _stockService.StockCodeExistsAsync(model.StockCode, cancellationToken: cancellationToken))
        {
            ModelState.AddModelError(nameof(model.StockCode), "Bu stok kodu zaten kullanılıyor.");
        }

        if (model.CategoryId.HasValue &&
            await _categoryService.GetByIdAsync(model.CategoryId.Value, cancellationToken) is null)
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Geçerli bir kategori seçin.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var saved = await _imageStorage.SaveAsync(model.ImageFile, cancellationToken);
            model.ImageUrl = saved?.ImageUrl;
            model.ThumbnailUrl = saved?.ThumbnailUrl;
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
            return View(model);
        }

        await _stockService.CreateAsync(StockViewModelMapper.ToStockItem(model), cancellationToken);

        TempData["SuccessMessage"] = "Stok kaydı başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var stock = await _stockService.GetByIdAsync(id, cancellationToken);
        if (stock is null)
        {
            return NotFound();
        }

        var model = StockViewModelMapper.ToFormViewModel(stock);
        await PopulateCategoriesAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StockFormViewModel model, CancellationToken cancellationToken)
    {
        await PopulateCategoriesAsync(model, cancellationToken);

        if (id != model.Id)
        {
            return BadRequest();
        }

        if (await _stockService.StockCodeExistsAsync(model.StockCode, model.Id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.StockCode), "Bu stok kodu zaten kullanılıyor.");
        }

        if (model.CategoryId.HasValue &&
            await _categoryService.GetByIdAsync(model.CategoryId.Value, cancellationToken) is null)
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Geçerli bir kategori seçin.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await _stockService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        model.ImageUrl = existing.ImageUrl;
        model.ThumbnailUrl = existing.ThumbnailUrl;

        if (model.ImageFile is not null && model.ImageFile.Length > 0)
        {
            try
            {
                var saved = await _imageStorage.SaveAsync(model.ImageFile, cancellationToken);
                if (saved is not null)
                {
                    _imageStorage.Delete(existing.ImageUrl);
                    model.ImageUrl = saved.ImageUrl;
                    model.ThumbnailUrl = saved.ThumbnailUrl;
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
                return View(model);
            }
        }

        var updated = await _stockService.UpdateAsync(StockViewModelMapper.ToStockItem(model), cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Stok kaydı başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var stock = await _stockService.GetByIdAsync(id, cancellationToken);
        if (stock is null)
        {
            return NotFound();
        }

        return View(stock);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var stock = await _stockService.GetByIdAsync(id, cancellationToken);
        if (stock is null)
        {
            return NotFound();
        }

        var deleted = await _stockService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Stok kaydı pasif duruma alındı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var restored = await _stockService.RestoreAsync(id, cancellationToken);
        if (!restored)
            return NotFound();

        TempData["SuccessMessage"] = "Stok kaydı tekrar aktif edildi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCategoriesAsync(StockFormViewModel model, CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllAsync(cancellationToken);
        model.Categories = categories
            .Where(c => c.IsActive || c.Id == model.CategoryId)
            .Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name,
            Selected = c.Id == model.CategoryId
        });
    }
}

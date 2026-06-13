using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VRLCRM.Application.Categories;
using VRLCRM.Application.Stocks;
using VRLCRM.Domain.Constants;
using VRLCRM.Models.Stocks;
using VRLCRM.Services;

namespace VRLCRM.Controllers;

[Authorize(Roles = AppRoles.AdminAndPersonel)]
public class StocksController : Controller
{
    private readonly IStockService _stockService;
    private readonly ICategoryService _categoryService;
    private readonly IStockImageStorage _imageStorage;

    public StocksController(
        IStockService stockService,
        ICategoryService categoryService,
        IStockImageStorage imageStorage)
    {
        _stockService = stockService;
        _categoryService = categoryService;
        _imageStorage = imageStorage;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var stocks = await _stockService.GetAllAsync(cancellationToken);
        return View(stocks);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var stock = await _stockService.GetByIdAsync(id, cancellationToken);
        if (stock is null)
        {
            return NotFound();
        }

        return View(stock);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new StockFormViewModel();
        await PopulateCategoriesAsync(model, cancellationToken);
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

        if (await _categoryService.GetByIdAsync(model.CategoryId, cancellationToken) is null)
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Geçerli bir kategori seçin.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            model.ImageUrl = await _imageStorage.SaveAsync(model.ImageFile, cancellationToken);
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

        if (await _categoryService.GetByIdAsync(model.CategoryId, cancellationToken) is null)
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

        if (model.ImageFile is not null && model.ImageFile.Length > 0)
        {
            try
            {
                var newImageUrl = await _imageStorage.SaveAsync(model.ImageFile, cancellationToken);
                if (newImageUrl is not null)
                {
                    _imageStorage.Delete(existing.ImageUrl);
                    model.ImageUrl = newImageUrl;
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

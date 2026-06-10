using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VRLCRM.Application.Categories;
using VRLCRM.Models.Categories;

namespace VRLCRM.Controllers;

public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllAsync(cancellationToken);
        return View(categories);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        return View(category);
    }

    public IActionResult Create()
    {
        return View(new CategoryFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (await _categoryService.NameExistsAsync(model.Name.Trim(), cancellationToken: cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Name), "Aynı isimde bir kategori mevcut.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _categoryService.CreateAsync(CategoryViewModelMapper.ToCategory(model), cancellationToken);

        TempData["SuccessMessage"] = "Kategori başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        return View(CategoryViewModelMapper.ToFormViewModel(category));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (await _categoryService.NameExistsAsync(model.Name.Trim(), model.Id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Name), "Aynı isimde bir kategori mevcut.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updated = await _categoryService.UpdateAsync(CategoryViewModelMapper.ToCategory(model), cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Kategori başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        ViewBag.HasStockItems = await _categoryService.HasStockItemsAsync(id, cancellationToken);
        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        if (await _categoryService.HasStockItemsAsync(id, cancellationToken))
        {
            TempData["ErrorMessage"] = "Bu kategoriye bağlı aktif stok kayıtları var. Önce stokları pasife alın veya başka kategoriye taşıyın.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        var deleted = await _categoryService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Kategori pasif duruma alındı.";
        return RedirectToAction(nameof(Index));
    }
}

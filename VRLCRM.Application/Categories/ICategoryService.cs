using VRLCRM.Domain.Entities;

namespace VRLCRM.Application.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);

    Task<bool> HasStockItemsAsync(int id, CancellationToken cancellationToken = default);

    Task<Category> CreateAsync(Category category, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Category category, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

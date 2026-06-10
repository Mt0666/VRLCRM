using VRLCRM.Domain.Entities;

namespace VRLCRM.Models.Categories;

public static class CategoryViewModelMapper
{
    public static CategoryFormViewModel ToFormViewModel(Category category)
    {
        return new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name
        };
    }

    public static Category ToCategory(CategoryFormViewModel model)
    {
        return new Category
        {
            Id = model.Id,
            Name = model.Name.Trim()
        };
    }
}

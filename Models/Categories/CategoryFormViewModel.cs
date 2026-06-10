using System.ComponentModel.DataAnnotations;

namespace VRLCRM.Models.Categories;

public class CategoryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    [Display(Name = "Kategori Adı")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}

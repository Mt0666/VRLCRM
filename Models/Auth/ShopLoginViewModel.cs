using System.ComponentModel.DataAnnotations;

namespace VRLCRM.Models.Auth;

public class ShopLoginViewModel
{
    [Required(ErrorMessage = "Telefon numarası gereklidir.")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [Display(Name = "Telefon")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gereklidir.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

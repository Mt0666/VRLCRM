using System.ComponentModel.DataAnnotations;

namespace VRLCRM.Models.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
    [Display(Name = "Email / Telefon")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gereklidir.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

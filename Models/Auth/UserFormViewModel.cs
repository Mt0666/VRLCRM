using System.ComponentModel.DataAnnotations;

namespace VRLCRM.Models.Auth;

public class UserFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Tam ad gereklidir.")]
    [Display(Name = "Tam Ad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon numarası gereklidir.")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [Display(Name = "Telefon")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gereklidir.")]
    [StringLength(100, ErrorMessage = "{0} en az {2} karakter uzunluğunda olmalıdır.", MinimumLength = 4)]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Şifre Onayı")]
    [Compare("Password", ErrorMessage = "Şifre ve onay şifresi eşleşmiyor.")]
    public string? ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Rol seçimi gereklidir.")]
    public string SelectedRole { get; set; } = string.Empty;
}

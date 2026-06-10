using System.ComponentModel.DataAnnotations;

namespace VRLCRM.Models.Customers;

public class CustomerFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    [Display(Name = "Ad")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad alanı zorunludur.")]
    [Display(Name = "Soyad")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Firma Adı")]
    [StringLength(200)]
    public string? CompanyName { get; set; }

    [Required(ErrorMessage = "Telefon numarası zorunludur.")]
    [Display(Name = "Telefon")]
    [StringLength(20)]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası girin.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Notlar")]
    [StringLength(2000)]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "İl alanı zorunludur.")]
    [Display(Name = "İl")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "İlçe alanı zorunludur.")]
    [Display(Name = "İlçe")]
    [StringLength(100)]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açık adres alanı zorunludur.")]
    [Display(Name = "Açık Adres")]
    [StringLength(500)]
    public string AddressLine { get; set; } = string.Empty;

    [Display(Name = "Cari Limit (₺)")]
    [Range(0, double.MaxValue, ErrorMessage = "Limit 0 veya daha büyük olmalıdır.")]
    public decimal? CreditLimit { get; set; }
}

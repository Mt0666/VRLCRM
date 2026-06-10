using System.ComponentModel.DataAnnotations;

namespace VRLCRM.Models.Suppliers;

public class SupplierFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Firma adı zorunludur.")]
    [Display(Name = "Firma Adı")]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Display(Name = "Yetkili Kişi")]
    [StringLength(100)]
    public string? ContactName { get; set; }

    [Required(ErrorMessage = "Telefon numarası zorunludur.")]
    [Display(Name = "Telefon")]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Vergi No")]
    [StringLength(20)]
    public string? TaxNumber { get; set; }

    [Display(Name = "Notlar")]
    [StringLength(2000)]
    public string? Notes { get; set; }

    [Display(Name = "Cari Limit (₺)")]
    [Range(0, double.MaxValue)]
    public decimal? CreditLimit { get; set; }

    [Display(Name = "İl")]
    [StringLength(100)]
    public string? City { get; set; }

    [Display(Name = "İlçe")]
    [StringLength(100)]
    public string? District { get; set; }

    [Display(Name = "Açık Adres")]
    [StringLength(500)]
    public string? AddressLine { get; set; }
}

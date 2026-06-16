using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VRLCRM.Models.Stocks;

public class StockFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Stok kodu zorunludur.")]
    [Display(Name = "Stok Kodu")]
    [StringLength(50)]
    public string StockCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
    [Display(Name = "Kategori")]
    public int CategoryId { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = [];

    [Display(Name = "Mevcut Görsel")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Ürün Görseli")]
    public IFormFile? ImageFile { get; set; }

    [Required(ErrorMessage = "Ürün adı zorunludur.")]
    [Display(Name = "Ürün Adı")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Fiyat zorunludur.")]
    [Display(Name = "Fiyat")]
    [Range(0, double.MaxValue, ErrorMessage = "Fiyat 0 veya daha büyük olmalıdır.")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "KDV Oranı zorunludur.")]
    [Display(Name = "KDV Oranı (%)")]
    [Range(0, 100, ErrorMessage = "KDV oranı 0-100 arasında olmalıdır.")]
    public decimal VatRate { get; set; }

    [Required(ErrorMessage = "Stok adedi zorunludur.")]
    [Display(Name = "Stok Adedi")]
    [Range(0, int.MaxValue, ErrorMessage = "Stok adedi 0 veya daha büyük olmalıdır.")]
    public int StockQuantity { get; set; }

    [Required(ErrorMessage = "Kritik stok seviyesi zorunludur.")]
    [Display(Name = "Kritik Stok Seviyesi")]
    [Range(0, int.MaxValue, ErrorMessage = "Kritik stok seviyesi 0 veya daha büyük olmalıdır.")]
    public int CriticalStockLevel { get; set; } = 5;

    [Display(Name = "Barkod")]
    [StringLength(100)]
    public string? Barcode { get; set; }

    [Display(Name = "Açıklama")]
    [StringLength(2000)]
    public string? Description { get; set; }
}

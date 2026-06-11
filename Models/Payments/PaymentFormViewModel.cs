using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Models.Payments;

public class PaymentFormViewModel
{
    public PaymentType Type { get; set; }

    [Display(Name = "Müşteri")]
    public int? CustomerId { get; set; }

    [Display(Name = "Tedarikçi")]
    public int? SupplierId { get; set; }

    [Required(ErrorMessage = "Tutar zorunludur.")]
    [Display(Name = "Tutar")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Tutar 0'dan büyük olmalıdır.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Ödeme Yöntemi seçilmelidir.")]
    [Display(Name = "Ödeme Yöntemi")]
    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;

    [Required(ErrorMessage = "Tarih zorunludur.")]
    [Display(Name = "Tarih")]
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Display(Name = "Notlar")]
    [StringLength(2000)]
    public string? Notes { get; set; }

    public IEnumerable<SelectListItem> Customers { get; set; } = [];
    public IEnumerable<SelectListItem> Suppliers { get; set; } = [];
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VRLCRM.Models.Orders;

public class OrderLineFormItem
{
    public int StockItemId { get; set; }

    public string StockName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, 100)]
    public decimal? VatRate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class OrderFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Cari Türü")]
    public string PartyType { get; set; } = "customer";

    [Display(Name = "Müşteri")]
    public int? CustomerId { get; set; }

    [Display(Name = "Tedarikçi")]
    public int? SupplierId { get; set; }

    [Display(Name = "Notlar")]
    [StringLength(2000)]
    public string? Notes { get; set; }

    [Display(Name = "Sipariş İskontosu (%)")]
    [Range(0, 100, ErrorMessage = "İskonto oranı 0-100 arasında olmalıdır.")]
    public decimal DiscountRate { get; set; }

    public List<OrderLineFormItem> Lines { get; set; } = [];

    public IEnumerable<SelectListItem> Customers { get; set; } = [];

    public IEnumerable<SelectListItem> Suppliers { get; set; } = [];
}

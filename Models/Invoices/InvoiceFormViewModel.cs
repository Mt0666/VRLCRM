using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Models.Invoices;

public class InvoiceLineFormItem
{
    public bool IsNewProduct { get; set; }

    public int StockItemId { get; set; }

    public string StockName { get; set; } = string.Empty;

    public string? NewStockCode { get; set; }

    public string? NewProductName { get; set; }

    public int? NewCategoryId { get; set; }

    public string? NewBarcode { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, 100)]
    public decimal VatRate { get; set; }
}

public class InvoiceStockOption
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string StockCode { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal VatRate { get; set; }
}

public class InvoiceCategoryOption
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class InvoiceFormViewModel
{
    public InvoiceType InvoiceType { get; set; }

    [Display(Name = "Müşteri")]
    public int? CustomerId { get; set; }

    [Display(Name = "Tedarikçi / Firma")]
    public int? SupplierId { get; set; }

    [Required]
    [Display(Name = "Fatura Tarihi")]
    [DataType(DataType.Date)]
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    [Display(Name = "Notlar")]
    [StringLength(2000)]
    public string? Notes { get; set; }

    public List<InvoiceLineFormItem> Lines { get; set; } = [];

    public IEnumerable<SelectListItem> Customers { get; set; } = [];

    public IEnumerable<SelectListItem> Suppliers { get; set; } = [];

    public IEnumerable<SelectListItem> Stocks { get; set; } = [];

    public IEnumerable<SelectListItem> Categories { get; set; } = [];

    public IReadOnlyList<InvoiceStockOption> StockOptions { get; set; } = [];

    public IReadOnlyList<InvoiceCategoryOption> CategoryOptions { get; set; } = [];
}

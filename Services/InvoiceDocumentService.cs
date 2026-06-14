using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;
using VRLCRM.Models.Settings;
using static VRLCRM.Services.CommercialDocumentPdfLayout;

namespace VRLCRM.Services;

public class InvoiceDocumentService
{
    private const string BrandColor = "#696CFF";
    private readonly CompanyDocumentSettings _company;
    private readonly string _logoPath;

    static InvoiceDocumentService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public InvoiceDocumentService(
        IOptions<CompanyDocumentSettings> companyOptions,
        IWebHostEnvironment environment)
    {
        _company = companyOptions.Value;
        _logoPath = ResolveLogoPath(environment.WebRootPath, _company);
    }

    public byte[] GeneratePdf(Invoice invoice)
    {
        var isSales = invoice.InvoiceType == InvoiceType.Sales;
        var typeText = GetTypeText(invoice.InvoiceType);
        var partyName = isSales
            ? invoice.Customer?.FullName ?? "-"
            : invoice.Supplier?.CompanyName ?? "-";
        var partyCompany = isSales ? invoice.Customer?.CompanyName : invoice.Supplier?.ContactName;
        var partyPhone = isSales
            ? invoice.Customer?.PhoneNumber ?? "-"
            : invoice.Supplier?.PhoneNumber ?? "-";

        var lines = invoice.Lines
            .Select(line => new DocumentLine(
                line.StockItem.StockCode,
                line.StockItem.Name,
                line.Quantity,
                line.UnitPrice,
                line.VatRate,
                line.VatAmount,
                line.LineTotal))
            .ToList();

        return BuildPdf(
            _company,
            _logoPath,
            typeText,
            invoice.InvoiceNumber,
            invoice.IsActive ? "Aktif" : "Pasif",
            invoice.InvoiceDate,
            "Fatura Tarihi",
            isSales ? "Müşteri" : "Tedarikçi",
            partyName,
            partyCompany,
            partyPhone,
            lines,
            new DocumentTotals(invoice.SubTotal, invoice.VatTotal, invoice.TotalAmount),
            invoice.Notes);
    }

    public byte[] GenerateExcel(Invoice invoice)
    {
        var typeText = GetTypeText(invoice.InvoiceType);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Fatura");

        sheet.Column(1).Width = 6;
        sheet.Column(2).Width = 14;
        sheet.Column(3).Width = 28;
        sheet.Column(4).Width = 8;
        sheet.Column(5).Width = 12;
        sheet.Column(6).Width = 10;
        sheet.Column(7).Width = 12;
        sheet.Column(8).Width = 12;

        sheet.Range("A1:H1").Merge().Value = $"{_company.CompanyName} - {typeText}";
        sheet.Range("A1:H1").Style.Font.SetBold().Font.SetFontSize(16)
            .Fill.SetBackgroundColor(XLColor.FromHtml(BrandColor)).Font.SetFontColor(XLColor.White);
        sheet.Range("A1:H1").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        sheet.Cell(3, 1).Value = "Fatura No";
        sheet.Cell(3, 2).Value = invoice.InvoiceNumber;
        sheet.Cell(4, 1).Value = "Tür";
        sheet.Cell(4, 2).Value = typeText;
        sheet.Cell(5, 1).Value = invoice.InvoiceType == InvoiceType.Sales ? "Müşteri" : "Tedarikçi";
        sheet.Cell(5, 2).Value = invoice.InvoiceType == InvoiceType.Sales
            ? invoice.Customer?.FullName ?? "-"
            : invoice.Supplier?.CompanyName ?? "-";
        sheet.Cell(6, 1).Value = "Tarih";
        sheet.Cell(6, 2).Value = invoice.InvoiceDate.ToLocalTime();
        sheet.Cell(6, 2).Style.DateFormat.SetFormat("dd.mm.yyyy");

        sheet.Range("A3:A6").Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#F4F4F8"));

        var headerRow = 8;
        sheet.Cell(headerRow, 1).Value = "#";
        sheet.Cell(headerRow, 2).Value = "Stok Kodu";
        sheet.Cell(headerRow, 3).Value = "Ürün";
        sheet.Cell(headerRow, 4).Value = "Adet";
        sheet.Cell(headerRow, 5).Value = "Birim Fiyat";
        sheet.Cell(headerRow, 6).Value = "KDV %";
        sheet.Cell(headerRow, 7).Value = "KDV Tutarı";
        sheet.Cell(headerRow, 8).Value = "Toplam";
        sheet.Range(headerRow, 1, headerRow, 8).Style.Font.SetBold()
            .Fill.SetBackgroundColor(XLColor.FromHtml(BrandColor)).Font.SetFontColor(XLColor.White);

        var row = headerRow + 1;
        var lineNo = 1;
        foreach (var line in invoice.Lines)
        {
            sheet.Cell(row, 1).Value = lineNo++;
            sheet.Cell(row, 2).Value = line.StockItem.StockCode;
            sheet.Cell(row, 3).Value = line.StockItem.Name;
            sheet.Cell(row, 4).Value = line.Quantity;
            sheet.Cell(row, 5).Value = line.UnitPrice;
            sheet.Cell(row, 6).Value = line.VatRate / 100m;
            sheet.Cell(row, 6).Style.NumberFormat.SetFormat("0%");
            sheet.Cell(row, 7).Value = line.VatAmount;
            sheet.Cell(row, 8).Value = line.LineTotal;
            sheet.Cell(row, 5).Style.NumberFormat.SetFormat("#,##0.00 \"₺\"");
            sheet.Cell(row, 7).Style.NumberFormat.SetFormat("#,##0.00 \"₺\"");
            sheet.Cell(row, 8).Style.NumberFormat.SetFormat("#,##0.00 \"₺\"");
            if (row % 2 == 0)
            {
                sheet.Range(row, 1, row, 8).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F8F8FB"));
            }
            row++;
        }

        row++;
        sheet.Range(row, 6, row, 7).Merge().Value = "Mal Hizmet Toplam Tutarı";
        sheet.Cell(row, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        sheet.Cell(row, 8).Value = invoice.SubTotal;
        sheet.Cell(row, 8).Style.NumberFormat.SetFormat("#,##0.00 \"₺\"");
        row++;
        sheet.Range(row, 6, row, 7).Merge().Value = "Hesaplanan KDV";
        sheet.Cell(row, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        sheet.Cell(row, 8).Value = invoice.VatTotal;
        sheet.Cell(row, 8).Style.NumberFormat.SetFormat("#,##0.00 \"₺\"");
        row++;
        sheet.Range(row, 6, row, 7).Merge().Value = "Ödenecek Tutar";
        sheet.Cell(row, 6).Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        sheet.Cell(row, 8).Value = invoice.TotalAmount;
        sheet.Cell(row, 8).Style.Font.SetBold().NumberFormat.SetFormat("#,##0.00 \"₺\"");

        if (!string.IsNullOrWhiteSpace(invoice.Notes))
        {
            row += 2;
            sheet.Cell(row, 1).Value = "Notlar";
            sheet.Cell(row, 1).Style.Font.SetBold();
            sheet.Range(row + 1, 1, row + 1, 8).Merge().Value = invoice.Notes;
        }

        sheet.SheetView.FreezeRows(headerRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string GetTypeText(InvoiceType type) => type switch
    {
        InvoiceType.Sales => "Satış Faturası",
        InvoiceType.Purchase => "Alış Faturası",
        _ => type.ToString()
    };
}

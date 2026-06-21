using ClosedXML.Excel;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VRLCRM.Domain.Entities;
using VRLCRM.Models.Settings;

namespace VRLCRM.Services;

public class StockDocumentService
{
    private const string BrandColor = "#696CFF";
    private const string LightBg = "#F4F4F8";
    private const string AltRowBg = "#F8F8FB";
    private const string DangerColor = "#EA5455";
    private const string SuccessColor = "#28C76F";
    private const string GrayColor = "#AAAAAA";
    private const string BlackColor = "#000000";
    private const string WhiteColor = "#FFFFFF";
    private const string GreyMediumColor = "#9E9E9E";
    private readonly CompanyDocumentSettings _company;

    static StockDocumentService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public StockDocumentService(IOptions<CompanyDocumentSettings> companyOptions)
    {
        _company = companyOptions.Value;
    }

    // ── Excel ─────────────────────────────────────────────────────────────
    public byte[] GenerateExcel(IReadOnlyList<StockItem> stocks)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Ürünler");

        sheet.Column(1).Width = 6;
        sheet.Column(2).Width = 14;
        sheet.Column(3).Width = 32;
        sheet.Column(4).Width = 18;
        sheet.Column(5).Width = 16;
        sheet.Column(6).Width = 12;
        sheet.Column(7).Width = 12;
        sheet.Column(8).Width = 10;
        sheet.Column(9).Width = 10;
        sheet.Column(10).Width = 10;
        sheet.Column(11).Width = 8;

        // Başlık
        var titleRange = sheet.Range("A1:K1");
        titleRange.Merge().Value = $"{_company.CompanyName} — Ürün Listesi";
        titleRange.Style
            .Font.SetBold()
            .Font.SetFontSize(14)
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(BrandColor))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        sheet.Row(1).Height = 28;

        // Tarih satırı
        sheet.Range("A2:K2").Merge().Value = $"Oluşturma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}";
        sheet.Range("A2:K2").Style
            .Font.SetItalic()
            .Font.SetFontColor(XLColor.Gray)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        // Header
        const int headerRow = 4;
        string[] headers = { "#", "Stok Kodu", "Ürün Adı", "Kategori", "Barkod", "Alış Fiyatı", "Satış Fiyatı", "KDV %", "Stok", "Kritik Seviye", "Durum" };
        for (int c = 0; c < headers.Length; c++)
        {
            sheet.Cell(headerRow, c + 1).Value = headers[c];
        }
        sheet.Range(headerRow, 1, headerRow, 11).Style
            .Font.SetBold()
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(BrandColor))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // Veriler
        int row = headerRow + 1;
        int no = 1;
        foreach (var s in stocks)
        {
            sheet.Cell(row, 1).Value = no++;
            sheet.Cell(row, 2).Value = s.StockCode;
            sheet.Cell(row, 3).Value = s.Name;
            sheet.Cell(row, 4).Value = s.Category?.Name ?? "";
            sheet.Cell(row, 5).Value = s.Barcode ?? "";
            sheet.Cell(row, 6).Value = s.PurchasePrice;
            sheet.Cell(row, 7).Value = s.Price;
            sheet.Cell(row, 8).Value = s.VatRate / 100m;
            sheet.Cell(row, 9).Value = s.StockQuantity;
            sheet.Cell(row, 10).Value = s.CriticalStockLevel;
            sheet.Cell(row, 11).Value = s.IsActive ? "Aktif" : "Pasif";

            sheet.Cell(row, 6).Style.NumberFormat.SetFormat("#,##0.00 \"₺\"");
            sheet.Cell(row, 7).Style.NumberFormat.SetFormat("#,##0.00 \"₺\"");
            sheet.Cell(row, 8).Style.NumberFormat.SetFormat("0%");
            sheet.Cell(row, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            if (s.IsCritical)
            {
                sheet.Cell(row, 9).Style.Font.SetFontColor(XLColor.Red).Font.SetBold();
            }

            if (!s.IsActive)
            {
                sheet.Range(row, 1, row, 11).Style.Font.SetFontColor(XLColor.Gray);
            }
            else if (row % 2 == 0)
            {
                sheet.Range(row, 1, row, 11).Style.Fill.SetBackgroundColor(XLColor.FromHtml(AltRowBg));
            }

            row++;
        }

        // Özet
        row++;
        sheet.Range(row, 1, row, 5).Merge().Value = $"Toplam {stocks.Count} ürün  |  Aktif: {stocks.Count(s => s.IsActive)}  |  Pasif: {stocks.Count(s => !s.IsActive)}";
        sheet.Range(row, 1, row, 5).Style
            .Font.SetBold()
            .Fill.SetBackgroundColor(XLColor.FromHtml(LightBg));

        sheet.SheetView.FreezeRows(headerRow);
        sheet.Range(headerRow, 1, row - 1, 11).Style
            .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorder(XLBorderStyleValues.Hair);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ── PDF ───────────────────────────────────────────────────────────────
    public byte[] GeneratePdf(IReadOnlyList<StockItem> stocks)
    {
        var company = _company;
        var now = DateTime.Now;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(8));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(company.CompanyName).Bold().FontSize(14).FontColor(BrandColor);
                            col.Item().Text("Ürün Listesi").FontSize(11).SemiBold();
                        });
                        row.ConstantItem(180).AlignRight().Column(col =>
                        {
                            col.Item().Text($"Tarih: {now:dd.MM.yyyy HH:mm}").FontSize(7).FontColor(GreyMediumColor);
                            col.Item().Text($"Toplam: {stocks.Count} ürün").FontSize(7).FontColor(GreyMediumColor);
                        });
                    });
                    header.Item().PaddingBottom(6).LineHorizontal(1).LineColor(BrandColor);
                });

                page.Content().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(22);   // #
                        cols.RelativeColumn(2);    // Stok Kodu
                        cols.RelativeColumn(4);    // Ürün Adı
                        cols.RelativeColumn(2.5f); // Kategori
                        cols.RelativeColumn(2);    // Barkod
                        cols.RelativeColumn(1.6f); // Alış Fiyatı
                        cols.RelativeColumn(1.6f); // Satış Fiyatı
                        cols.ConstantColumn(38);   // KDV %
                        cols.ConstantColumn(38);   // Stok
                        cols.ConstantColumn(48);   // Durum
                    });

                    static IContainer HeaderCell(IContainer c) =>
                        c.Background(BrandColor).Padding(4).AlignMiddle();

                    table.Header(h =>
                    {
                        foreach (var title in new[] { "#", "Stok Kodu", "Ürün Adı", "Kategori", "Barkod", "Alış Fiyatı", "Satış Fiyatı", "KDV %", "Stok", "Durum" })
                        {
                            h.Cell().Element(HeaderCell).Text(title).Bold().FontSize(7.5f).FontColor(WhiteColor);
                        }
                    });

                    int no = 1;
                    foreach (var s in stocks)
                    {
                        var bg = no % 2 == 0 ? AltRowBg : WhiteColor;
                        var textColor = s.IsActive ? BlackColor : GrayColor;

                        IContainer DataCell(IContainer c) =>
                            c.Background(bg).Padding(3).AlignMiddle();

                        table.Cell().Element(DataCell).Text($"{no++}").FontColor(textColor).FontSize(7.5f);
                        table.Cell().Element(DataCell).Text(s.StockCode).FontColor(textColor).FontSize(7.5f);
                        table.Cell().Element(DataCell).Text(s.Name).FontColor(textColor).FontSize(7.5f);
                        table.Cell().Element(DataCell).Text(s.Category?.Name ?? "").FontColor(textColor).FontSize(7.5f);
                        table.Cell().Element(DataCell).Text(s.Barcode ?? "").FontColor(textColor).FontSize(7.5f);
                        table.Cell().Element(DataCell).AlignRight().Text($"{s.PurchasePrice:N2} ₺").FontColor(textColor).FontSize(7.5f);
                        table.Cell().Element(DataCell).AlignRight().Text($"{s.Price:N2} ₺").Bold().FontColor(textColor).FontSize(7.5f);
                        table.Cell().Element(DataCell).AlignCenter().Text($"%{s.VatRate:N0}").FontColor(textColor).FontSize(7.5f);

                        // Kritik stok — kırmızı + kalın
                        var stockColor = s.IsCritical ? DangerColor : textColor;
                        if (s.IsCritical)
                        {
                            table.Cell().Element(DataCell).AlignCenter()
                                .Text($"{s.StockQuantity}").Bold().FontColor(stockColor).FontSize(7.5f);
                        }
                        else
                        {
                            table.Cell().Element(DataCell).AlignCenter()
                                .Text($"{s.StockQuantity}").FontColor(stockColor).FontSize(7.5f);
                        }

                        var statusColor = s.IsActive ? SuccessColor : GrayColor;
                        table.Cell().Element(DataCell).AlignCenter()
                            .Text(s.IsActive ? "Aktif" : "Pasif").FontColor(statusColor).FontSize(7.5f);
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Sayfa ").FontSize(7).FontColor(GreyMediumColor);
                    t.CurrentPageNumber().FontSize(7).FontColor(GreyMediumColor);
                    t.Span(" / ").FontSize(7).FontColor(GreyMediumColor);
                    t.TotalPages().FontSize(7).FontColor(GreyMediumColor);
                });
            });
        });

        return doc.GeneratePdf();
    }
}

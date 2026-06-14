using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VRLCRM.Models.Settings;

namespace VRLCRM.Services;

public static class CommercialDocumentPdfLayout
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    private const string BrandColor = "#696CFF";
    private const string MutedColor = "#6F6B7D";
    private const string BorderColor = "#E4E4E8";

    public record DocumentLine(
        string StockCode,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal VatRate,
        decimal VatAmount,
        decimal LineTotal);

    public record DocumentTotals(
        decimal SubTotal,
        decimal VatTotal,
        decimal TotalAmount);

    public static byte[] BuildPdf(
        CompanyDocumentSettings company,
        string? logoPath,
        string documentSubtitle,
        string documentNumber,
        string? statusLabel,
        DateTime documentDate,
        string dateLabel,
        string partyLabel,
        string partyName,
        string? partyCompany,
        string partyPhone,
        IReadOnlyList<DocumentLine> lines,
        DocumentTotals totals,
        string? notes)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Row(brandRow =>
                            {
                                if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                                {
                                    brandRow.ConstantItem(38).Height(38).Image(logoPath).FitArea();
                                }
                                else
                                {
                                    brandRow.ConstantItem(38).Height(38).Background(BrandColor)
                                        .AlignCenter().AlignMiddle()
                                        .Text(GetInitials(company.CompanyName))
                                        .FontSize(13).SemiBold().FontColor(Colors.White);
                                }

                                brandRow.RelativeItem().PaddingLeft(10).AlignMiddle().Column(brandText =>
                                {
                                    brandText.Item().Text(company.CompanyName).FontSize(20).SemiBold().FontColor(BrandColor);
                                    brandText.Item().Text(documentSubtitle).FontSize(12).FontColor(MutedColor);
                                });
                            });
                        });

                        row.ConstantItem(180).AlignRight().Column(right =>
                        {
                            right.Item().Text(documentNumber).FontSize(14).SemiBold();
                            if (!string.IsNullOrWhiteSpace(statusLabel))
                            {
                                right.Item().Text(statusLabel).FontColor(MutedColor);
                            }
                        });
                    });

                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().Background(Colors.Grey.Lighten4).Padding(12).Row(infoRow =>
                    {
                        infoRow.RelativeItem().Column(c =>
                        {
                            c.Item().Text(partyLabel).SemiBold();
                            c.Item().Text(partyName);
                            if (!string.IsNullOrWhiteSpace(partyCompany))
                            {
                                c.Item().Text(partyCompany).FontColor(MutedColor);
                            }
                        });
                        infoRow.RelativeItem().Column(c =>
                        {
                            c.Item().Text(dateLabel).SemiBold();
                            c.Item().Text(documentDate.ToLocalTime().ToString("dd.MM.yyyy HH:mm", TurkishCulture));
                        });
                        infoRow.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Telefon").SemiBold();
                            c.Item().Text(string.IsNullOrWhiteSpace(partyPhone) ? "-" : partyPhone);
                        });
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(22);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(2.2f);
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.2f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(BrandColor).Padding(6).Text("#").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).Text("Stok Kodu").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).Text("Ürün").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).AlignCenter().Text("Adet").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).AlignRight().Text("Birim Fiyat").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).AlignCenter().Text("KDV %").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).AlignRight().Text("KDV Tutarı").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).AlignRight().Text("Toplam").FontColor(Colors.White).SemiBold();
                        });

                        var index = 1;
                        foreach (var line in lines)
                        {
                            var bg = index % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;
                            table.Cell().Background(bg).Padding(6).Text(index.ToString());
                            table.Cell().Background(bg).Padding(6).Text(line.StockCode);
                            table.Cell().Background(bg).Padding(6).Text(line.ProductName);
                            table.Cell().Background(bg).Padding(6).AlignCenter().Text(line.Quantity.ToString());
                            table.Cell().Background(bg).Padding(6).AlignRight().Text(FormatMoney(line.UnitPrice));
                            table.Cell().Background(bg).Padding(6).AlignCenter().Text($"%{line.VatRate:N0}");
                            table.Cell().Background(bg).Padding(6).AlignRight().Text(FormatMoney(line.VatAmount));
                            table.Cell().Background(bg).Padding(6).AlignRight().Text(FormatMoney(line.LineTotal));
                            index++;
                        }
                    });

                    column.Item().AlignRight().Background(Colors.Grey.Lighten4).Padding(12).Width(280).Column(totalBox =>
                    {
                        totalBox.Item().Text("Genel Toplam").FontSize(12).SemiBold();
                        totalBox.Item().PaddingTop(8).Row(r =>
                        {
                            r.RelativeItem().Text("Mal Hizmet Toplam Tutarı").FontColor(MutedColor).FontSize(9);
                            r.ConstantItem(90).AlignRight().Text(FormatMoney(totals.SubTotal)).FontSize(9);
                        });
                        totalBox.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Hesaplanan KDV").FontColor(MutedColor).FontSize(9);
                            r.ConstantItem(90).AlignRight().Text(FormatMoney(totals.VatTotal)).FontSize(9);
                        });
                        totalBox.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Vergiler Dahil Toplam Tutar").FontColor(MutedColor).FontSize(9);
                            r.ConstantItem(90).AlignRight().Text(FormatMoney(totals.TotalAmount)).FontSize(9);
                        });
                        totalBox.Item().PaddingTop(6).BorderTop(1).BorderColor(BorderColor).PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text("Ödenecek Tutar").SemiBold();
                            r.ConstantItem(90).AlignRight().Text(FormatMoney(totals.TotalAmount)).SemiBold();
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(notes))
                    {
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(note =>
                        {
                            note.Item().Text("Notlar").SemiBold();
                            note.Item().Text(notes);
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Oluşturulma: ").FontColor(MutedColor).FontSize(8);
                    text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm", TurkishCulture)).FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }

    public static string FormatMoney(decimal amount) =>
        amount.ToString("N2", TurkishCulture) + " ₺";

    public static string FormatTurkishDate(DateTime date) =>
        date.ToLocalTime().ToString("dd MMMM yyyy", TurkishCulture);

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "MV";
        }

        if (parts.Length == 1)
        {
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        }

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    public static string FormatAddress(string? city, string? district, string? addressLine)
    {
        var parts = new[] { addressLine, district, city }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        return parts.Count == 0 ? string.Empty : string.Join(", ", parts);
    }

    public static string ResolveLogoPath(string webRootPath, CompanyDocumentSettings company) =>
        Path.Combine(webRootPath, company.LogoRelativePath);
}

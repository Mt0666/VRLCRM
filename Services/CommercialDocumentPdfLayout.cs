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
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal VatRate,
        decimal VatAmount,
        decimal LineTotal,
        string? LineNotes = null,
        string? StockCode = null,
        string? Barcode = null,
        string? ProductDescription = null);

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
        string? notes,
        decimal discountRate = 0,
        decimal discountAmount = 0,
        decimal? partyBalance = null)
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
                            columns.RelativeColumn(3.4f);   // Ürün
                            columns.RelativeColumn(0.5f);   // Adet
                            columns.RelativeColumn(0.9f);   // Birim Fiyat
                            columns.RelativeColumn(0.7f);   // KDV %
                            columns.RelativeColumn(1.0f);   // KDV Tutarı
                            columns.RelativeColumn(0.9f);   // Toplam
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(BrandColor).PaddingVertical(4).PaddingHorizontal(6).Text("Ürün").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).PaddingVertical(4).PaddingHorizontal(6).AlignCenter().Text("Adet").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).PaddingVertical(4).PaddingHorizontal(6).AlignRight().Text("Birim Fiyat").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).PaddingVertical(4).PaddingHorizontal(6).AlignCenter().Text("KDV %").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).PaddingVertical(4).PaddingHorizontal(6).AlignRight().Text("KDV Tutarı").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).PaddingVertical(4).PaddingHorizontal(6).AlignRight().Text("Toplam").FontColor(Colors.White).SemiBold();
                        });

                        var index = 1;
                        foreach (var line in lines)
                        {
                            var bg = index % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(6).Column(productCol =>
                            {
                                productCol.Item().Text(line.ProductName);
                                if (!string.IsNullOrWhiteSpace(line.LineNotes))
                                {
                                    productCol.Item().PaddingTop(2).Text(line.LineNotes).FontSize(8).FontColor(MutedColor).Italic();
                                }
                            });
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(6).AlignCenter().Text(line.Quantity.ToString());
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(6).AlignRight().Text(FormatMoney(line.UnitPrice));
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(6).AlignCenter().Text($"%{line.VatRate:N0}");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(6).AlignRight().Text(FormatMoney(line.VatAmount));
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(6).AlignRight().Text(FormatMoney(line.LineTotal));
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
                        if (discountRate > 0)
                        {
                            totalBox.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().Text($"Sipariş İskontosu (%{discountRate:N2})").FontColor(MutedColor).FontSize(9);
                                r.ConstantItem(90).AlignRight().Text("-" + FormatMoney(discountAmount)).FontSize(9);
                            });
                        }
                        totalBox.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Vergiler Dahil Toplam Tutar").FontColor(MutedColor).FontSize(9);
                            r.ConstantItem(90).AlignRight().Text(FormatMoney(totals.TotalAmount + discountAmount)).FontSize(9);
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

                page.Footer().Row(f =>
                {
                    f.RelativeItem().AlignLeft().Text(text =>
                    {
                        if (partyBalance.HasValue)
                        {
                            text.Span("Güncel Bakiye: ").FontColor(MutedColor).FontSize(9).SemiBold();
                            text.Span(FormatMoney(partyBalance.Value)).FontSize(9).SemiBold();
                        }
                    });
                    f.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Oluşturulma: ").FontColor(MutedColor).FontSize(8);
                        text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm", TurkishCulture)).FontSize(8);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    public static byte[] BuildSimplifiedDocumentPdf(
        string documentNumber,
        string? statusLabel,
        DateTime documentDate,
        string partyLabel,
        string partyName,
        string? partyCompany,
        string partyPhone,
        IReadOnlyList<DocumentLine> lines,
        DocumentTotals totals,
        decimal? partyBalance = null)
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
                            c.Item().Text("Tarih").SemiBold();
                            c.Item().Text(documentDate.ToLocalTime().ToString("dd.MM.yyyy", TurkishCulture));
                        });
                        infoRow.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Telefon").SemiBold();
                            c.Item().Text(string.IsNullOrWhiteSpace(partyPhone) ? "-" : partyPhone);
                        });
                        infoRow.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Belge No").SemiBold();
                            c.Item().Text(documentNumber);
                            if (!string.IsNullOrWhiteSpace(statusLabel))
                            {
                                c.Item().Text(statusLabel).FontSize(8).FontColor(MutedColor);
                            }
                        });
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4.2f);   // Ürün
                            columns.RelativeColumn(0.5f);   // Adet
                            columns.RelativeColumn(0.9f);   // Birim Fiyat
                            columns.RelativeColumn(0.9f);   // Toplam
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(BrandColor).PaddingVertical(4).PaddingHorizontal(6).Text("Ürün").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).PaddingVertical(4).PaddingHorizontal(6).AlignCenter().Text("Adet").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).PaddingVertical(4).PaddingHorizontal(6).AlignRight().Text("Birim Fiyat").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).PaddingVertical(4).PaddingHorizontal(6).AlignRight().Text("Toplam").FontColor(Colors.White).SemiBold();
                        });

                        var index = 1;
                        foreach (var line in lines)
                        {
                            var bg = index % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;
                            var hasDetail = HasProductDetail(line);
                            // Detay satırı varsa çizgi/alt boşluk detay satırına gider; yoksa ana satıra.
                            var mainBottom = hasDetail ? 1f : 3f;
                            var mainLine = hasDetail ? 0f : 0.5f;

                            table.Cell().Background(bg).BorderBottom(mainLine).BorderColor(Colors.Grey.Lighten2).PaddingHorizontal(6).PaddingTop(3).PaddingBottom(mainBottom).Text(line.ProductName);
                            table.Cell().Background(bg).BorderBottom(mainLine).BorderColor(Colors.Grey.Lighten2).PaddingHorizontal(6).PaddingTop(3).PaddingBottom(mainBottom).AlignCenter().Text(line.Quantity.ToString());
                            table.Cell().Background(bg).BorderBottom(mainLine).BorderColor(Colors.Grey.Lighten2).PaddingHorizontal(6).PaddingTop(3).PaddingBottom(mainBottom).AlignRight().Text(FormatMoney(line.UnitPrice));
                            table.Cell().Background(bg).BorderBottom(mainLine).BorderColor(Colors.Grey.Lighten2).PaddingHorizontal(6).PaddingTop(3).PaddingBottom(mainBottom).AlignRight().Text(FormatMoney(line.LineTotal));

                            if (hasDetail)
                            {
                                table.Cell().ColumnSpan(4).Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingHorizontal(6).PaddingTop(0).PaddingBottom(3).Text(text =>
                                {
                                    RenderProductDetailText(text, line);
                                });
                            }

                            index++;
                        }
                    });

                    column.Item().Row(totalRow =>
                    {
                        totalRow.RelativeItem().AlignLeft().AlignMiddle().Text(text =>
                        {
                            if (partyBalance.HasValue)
                            {
                                text.Span("Güncel Bakiye: ").FontColor(MutedColor).SemiBold();
                                text.Span(FormatMoney(partyBalance.Value)).SemiBold();
                            }
                        });
                        totalRow.ConstantItem(280).Background(Colors.Grey.Lighten4).Padding(12).Row(r =>
                        {
                            r.RelativeItem().Text("Ödenecek Tutar").SemiBold();
                            r.ConstantItem(90).AlignRight().Text(FormatMoney(totals.TotalAmount)).SemiBold();
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span(DateTime.Now.ToString("dd.MM.yyyy", TurkishCulture)).FontSize(8).FontColor(MutedColor);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static bool HasProductDetail(DocumentLine line) =>
        !string.IsNullOrWhiteSpace(line.StockCode)
        || !string.IsNullOrWhiteSpace(line.Barcode)
        || !string.IsNullOrWhiteSpace(line.LineNotes)
        || !string.IsNullOrWhiteSpace(line.ProductDescription);

    private static void RenderProductDetailText(TextDescriptor text, DocumentLine line)
    {
        text.DefaultTextStyle(x => x.FontSize(8).FontColor(MutedColor).Italic());

        var hasStockCode = !string.IsNullOrWhiteSpace(line.StockCode);
        var hasBarcode = !string.IsNullOrWhiteSpace(line.Barcode);
        var description = !string.IsNullOrWhiteSpace(line.LineNotes)
            ? line.LineNotes
            : line.ProductDescription;
        var hasDescription = !string.IsNullOrWhiteSpace(description);

        if (hasStockCode)
        {
            text.Span($"Stok Kodu: {line.StockCode}");
        }

        if (hasBarcode)
        {
            if (hasStockCode)
            {
                text.Span("  |  ");
            }

            text.Span($"Barkod: {line.Barcode}");
        }

        if (hasDescription)
        {
            text.Span("     ");
            text.Span($"Açıklama: {description}");
        }
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

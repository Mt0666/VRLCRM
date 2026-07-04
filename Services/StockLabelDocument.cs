using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace VRLCRM.Services;

/// <summary>4×6 inç ürün etiketi PDF'i (ARGOX vb. etiket yazıcıları için).</summary>
public static class StockLabelDocument
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    static StockLabelDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(
        string name,
        string stockCode,
        string barcodeText,
        byte[] barcodePng,
        decimal price)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(4, 6, Unit.Inch);
                page.Margin(0.18f, Unit.Inch);
                page.DefaultTextStyle(x => x.FontSize(14));

                page.Content().Column(col =>
                {
                    col.Spacing(6);

                    col.Item().AlignCenter().Text(name).FontSize(26).Bold();
                    col.Item().AlignCenter().Text($"Stok Kodu: {stockCode}").FontSize(15).SemiBold();

                    if (barcodePng.Length > 0)
                    {
                        col.Item().PaddingTop(10).AlignCenter().Width(3.4f, Unit.Inch).Image(barcodePng);
                        col.Item().AlignCenter().Text(barcodeText).FontSize(15);
                    }
                });

                page.Footer().BorderTop(2).PaddingTop(8).AlignCenter()
                    .Text($"{price.ToString("N2", TurkishCulture)} ₺").FontSize(52).Bold();
            });
        }).GeneratePdf();
    }
}

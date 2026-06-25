using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VRLCRM.Models.Accounts;
using static VRLCRM.Services.CommercialDocumentPdfLayout;

namespace VRLCRM.Services;

public class AccountStatementDocumentService
{
    private const string BrandColor = "#696CFF";
    private const string MutedColor = "#6F6B7D";

    static AccountStatementDocumentService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdf(string partyLabel, string partyName, IReadOnlyList<AccountTransactionRow> rows)
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
                        row.RelativeItem().Text("Cari Hareketler").FontSize(18).SemiBold().FontColor(BrandColor);
                        row.ConstantItem(200).AlignRight().Column(right =>
                        {
                            right.Item().Text(partyLabel).FontSize(12).SemiBold();
                            right.Item().Text(partyName).FontColor(MutedColor);
                        });
                    });

                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    if (!rows.Any())
                    {
                        column.Item().PaddingVertical(24).AlignCenter()
                            .Text("Kayıtlı hareket bulunamadı.").FontColor(MutedColor);
                        return;
                    }

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(2f);
                            columns.RelativeColumn(1f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(BrandColor).Padding(6).Text("No").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).Text("Tarih").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).Text("Tür").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).AlignRight().Text("Tutar").FontColor(Colors.White).SemiBold();
                        });

                        var index = 0;
                        foreach (var row in rows)
                        {
                            var bg = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                            table.Cell().Background(bg).Padding(6).Text(row.Number);
                            table.Cell().Background(bg).Padding(6).Text(row.Date.ToLocalTime().ToString("dd.MM.yyyy"));
                            table.Cell().Background(bg).Padding(6).Text(row.TypeLabel);
                            table.Cell().Background(bg).Padding(6).AlignRight().Text(FormatMoney(row.Amount));
                            index++;
                        }
                    });

                    column.Item().AlignRight().PaddingTop(8).Text(text =>
                    {
                        text.Span("Toplam Tutar: ").SemiBold();
                        text.Span(FormatMoney(rows.Sum(r => r.Amount))).SemiBold();
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Oluşturulma: ").FontColor(MutedColor).FontSize(8);
                    text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm")).FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }
}

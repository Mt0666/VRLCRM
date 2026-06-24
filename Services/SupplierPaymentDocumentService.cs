using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;
using VRLCRM.Helpers;
using VRLCRM.Models.Settings;
using static VRLCRM.Services.CommercialDocumentPdfLayout;

namespace VRLCRM.Services;

public class SupplierPaymentDocumentService
{
    private const string BrandColor = "#696CFF";
    private const string MutedColor = "#6F6B7D";
    private readonly CompanyDocumentSettings _company;
    private readonly string _logoPath;

    static SupplierPaymentDocumentService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public SupplierPaymentDocumentService(
        IOptions<CompanyDocumentSettings> companyOptions,
        IWebHostEnvironment environment)
    {
        _company = companyOptions.Value;
        _logoPath = ResolveLogoPath(environment.WebRootPath, _company);
    }

    public byte[] GeneratePdf(Supplier supplier, IReadOnlyList<Payment> payments)
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
                        row.RelativeItem().Text(_company.CompanyName).FontSize(18).SemiBold().FontColor(BrandColor);
                        row.ConstantItem(180).AlignRight().Column(right =>
                        {
                            right.Item().Text("Ödeme Listesi").FontSize(14).SemiBold();
                            right.Item().Text(supplier.CompanyName).FontColor(MutedColor);
                        });
                    });

                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(2f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(BrandColor).Padding(6).Text("Fiş No").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).Text("Tür").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).Text("Tarih").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).AlignRight().Text("Tutar").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).Text("Yöntem").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).Text("Not").FontColor(Colors.White).SemiBold();
                        });

                        var index = 0;
                        foreach (var payment in payments)
                        {
                            var bg = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                            table.Cell().Background(bg).Padding(6).Text(payment.PaymentNumber);
                            table.Cell().Background(bg).Padding(6).Text(PaymentDisplayHelper.GetPaymentTypeLabel(payment));
                            table.Cell().Background(bg).Padding(6).Text(payment.PaymentDate.ToLocalTime().ToString("dd.MM.yyyy"));
                            table.Cell().Background(bg).Padding(6).AlignRight().Text(FormatMoney(payment.Amount));
                            table.Cell().Background(bg).Padding(6).Text(GetMethodLabel(payment.Method));
                            table.Cell().Background(bg).Padding(6).Text(payment.Notes ?? "-");
                            index++;
                        }
                    });

                    column.Item().AlignRight().PaddingTop(8).Text(text =>
                    {
                        text.Span("Toplam Hareket: ").SemiBold();
                        text.Span(FormatMoney(payments.Sum(p => p.Amount))).SemiBold();
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

    private static string GetMethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Nakit",
        PaymentMethod.BankTransfer => "Havale/EFT",
        PaymentMethod.CreditCard => "Kredi Kartı",
        PaymentMethod.Check => "Çek",
        _ => method.ToString()
    };
}

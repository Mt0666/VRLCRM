using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Services;

public class OrderDocumentService
{
    private const string BrandColor = "#696CFF";
    private const string MutedColor = "#6F6B7D";

    static OrderDocumentService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdf(Order order)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("VRLCRM").FontSize(20).SemiBold().FontColor(BrandColor);
                            left.Item().Text("Sipariş Formu").FontSize(12).FontColor(MutedColor);
                        });
                        row.ConstantItem(180).AlignRight().Column(right =>
                        {
                            right.Item().Text(order.OrderNumber).FontSize(14).SemiBold();
                            right.Item().Text(GetStatusText(order.Status)).FontColor(MutedColor);
                        });
                    });
                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(16).Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Background(Colors.Grey.Lighten4).Padding(12).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Müşteri").SemiBold();
                            c.Item().Text(order.Customer.FullName);
                            if (!string.IsNullOrWhiteSpace(order.Customer.CompanyName))
                            {
                                c.Item().Text(order.Customer.CompanyName).FontColor(MutedColor);
                            }
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Sipariş Tarihi").SemiBold();
                            c.Item().Text(order.OrderDate.ToLocalTime().ToString("dd.MM.yyyy HH:mm"));
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Telefon").SemiBold();
                            c.Item().Text(order.Customer.PhoneNumber);
                        });
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(28);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(BrandColor).Padding(6).Text("#").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).Text("Ürün").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).AlignCenter().Text("Adet").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).AlignRight().Text("Birim Fiyat").FontColor(Colors.White).SemiBold();
                            header.Cell().Background(BrandColor).Padding(6).AlignRight().Text("Toplam").FontColor(Colors.White).SemiBold();
                        });

                        var index = 1;
                        foreach (var line in order.Lines)
                        {
                            var bg = index % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;
                            table.Cell().Background(bg).Padding(6).Text(index.ToString());
                            table.Cell().Background(bg).Padding(6).Text(line.StockItem.Name);
                            table.Cell().Background(bg).Padding(6).AlignCenter().Text(line.Quantity.ToString());
                            table.Cell().Background(bg).Padding(6).AlignRight().Text($"{line.UnitPrice:N2} ₺");
                            table.Cell().Background(bg).Padding(6).AlignRight().Text($"{line.LineTotal:N2} ₺");
                            index++;
                        }
                    });

                    column.Item().AlignRight().Background(Colors.Grey.Lighten4).Padding(12).Column(total =>
                    {
                        total.Item().Text($"Genel Toplam: {order.TotalAmount:N2} ₺").FontSize(14).SemiBold();
                    });

                    if (!string.IsNullOrWhiteSpace(order.Notes))
                    {
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(note =>
                        {
                            note.Item().Text("Notlar").SemiBold();
                            note.Item().Text(order.Notes);
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Oluşturulma: ").FontColor(MutedColor);
                    text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateExcel(Order order)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sipariş");

        sheet.Column(1).Width = 18;
        sheet.Column(2).Width = 34;
        sheet.Column(3).Width = 10;
        sheet.Column(4).Width = 14;
        sheet.Column(5).Width = 14;

        sheet.Range("A1:E1").Merge().Value = "VRLCRM - Sipariş Formu";
        sheet.Range("A1:E1").Style.Font.SetBold().Font.SetFontSize(16).Fill.SetBackgroundColor(XLColor.FromHtml(BrandColor)).Font.SetFontColor(XLColor.White);
        sheet.Range("A1:E1").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        sheet.Cell(3, 1).Value = "Sipariş No";
        sheet.Cell(3, 2).Value = order.OrderNumber;
        sheet.Cell(4, 1).Value = "Müşteri";
        sheet.Cell(4, 2).Value = order.Customer.FullName;
        sheet.Cell(5, 1).Value = "Tarih";
        sheet.Cell(5, 2).Value = order.OrderDate.ToLocalTime();
        sheet.Cell(5, 2).Style.DateFormat.SetFormat("dd.mm.yyyy hh:mm");
        sheet.Cell(6, 1).Value = "Durum";
        sheet.Cell(6, 2).Value = GetStatusText(order.Status);

        sheet.Range("A3:A6").Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#F4F4F8"));

        var headerRow = 8;
        sheet.Cell(headerRow, 1).Value = "#";
        sheet.Cell(headerRow, 2).Value = "Ürün";
        sheet.Cell(headerRow, 3).Value = "Adet";
        sheet.Cell(headerRow, 4).Value = "Birim Fiyat";
        sheet.Cell(headerRow, 5).Value = "Toplam";
        sheet.Range(headerRow, 1, headerRow, 5).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml(BrandColor)).Font.SetFontColor(XLColor.White);

        var row = headerRow + 1;
        var lineNo = 1;
        foreach (var line in order.Lines)
        {
            sheet.Cell(row, 1).Value = lineNo++;
            sheet.Cell(row, 2).Value = line.StockItem.Name;
            sheet.Cell(row, 3).Value = line.Quantity;
            sheet.Cell(row, 4).Value = line.UnitPrice;
            sheet.Cell(row, 5).Value = line.LineTotal;
            sheet.Cell(row, 4).Style.NumberFormat.SetFormat("#,##0.00 \"₺\"");
            sheet.Cell(row, 5).Style.NumberFormat.SetFormat("#,##0.00 \"₺\"");
            if (row % 2 == 0)
            {
                sheet.Range(row, 1, row, 5).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F8F8FB"));
            }
            row++;
        }

        row++;
        sheet.Range(row, 3, row, 4).Merge().Value = "Genel Toplam";
        sheet.Cell(row, 3).Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        sheet.Cell(row, 5).Value = order.TotalAmount;
        sheet.Cell(row, 5).Style.Font.SetBold().NumberFormat.SetFormat("#,##0.00 \"₺\"");

        if (!string.IsNullOrWhiteSpace(order.Notes))
        {
            row += 2;
            sheet.Cell(row, 1).Value = "Notlar";
            sheet.Cell(row, 1).Style.Font.SetBold();
            sheet.Range(row + 1, 1, row + 1, 5).Merge().Value = order.Notes;
        }

        sheet.SheetView.FreezeRows(headerRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string GetStatusText(OrderStatus status) => status switch
    {
        OrderStatus.Approved => "Onaylandı",
        OrderStatus.Cancelled => "İptal",
        _ => status.ToString()
    };
}

using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NextWave.Erp.Sales.Pdf
{

    public class SalesPosPdf1(PdfForSalesPosModel data, bool isTi) : IDocument
    {
        public PdfForSalesPosModel ObjPdfData { get; } = data;

        public DocumentMetadata GetMetadata()
        {
            return DocumentMetadata.Default;
        }

        public DocumentSettings GetSettings()
        {
            return DocumentSettings.Default;
        }

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.MarginLeft(2);
                    page.MarginRight(2);
                    page.ContinuousSize(200f);
                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                });
        }

        private void ComposeHeader(IContainer container)
        {
            var model = ObjPdfData;
            container.PaddingBottom(1).Row(row =>
            {
                row.RelativeItem().PaddingTop(10).Column(column =>
                {
                    if (model.BranchName.Trim().Length > 0)
                    {
                        column.Item().Text(text =>
                        {
                            text.AlignCenter();
                            text.Span($"{model.BranchName}").FontSize(8).Bold();
                        });
                        if (!string.IsNullOrWhiteSpace(model.Address))
                            column.Item().Text(text =>
                            {
                                text.AlignCenter();
                                text.Span($"{model.Address}").FontSize(8).Bold();
                            });
                        if (!string.IsNullOrWhiteSpace(model.BranchPhone))
                            column.Item().Text(text =>
                            {
                                text.AlignCenter();
                                text.Span($"PHONE:{model.BranchPhone}").FontSize(8).Bold();
                            });
                        if (!string.IsNullOrWhiteSpace(model.BranchPan))
                            column.Item().Text(text =>
                            {
                                text.AlignCenter();
                                text.Span($"PAN : {model.BranchPan}").FontSize(8).Bold();
                            });
                        column.Item().Text(text =>
                        {
                            text.AlignCenter();
                            if (isTi)
                                if (model.IsInvoice)
                                    text.Span("INVOICE").FontSize(8).Bold();
                                else
                                    text.Span("TAX INVOICE").FontSize(8).Bold();
                            else
                                text.Span("ABBREVIATED TAX INVOICE").FontSize(8).Bold();
                        });
                    }

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    column.Item().Row(col =>
                    {
                        col.RelativeItem().Text(text =>
                        {
                            text.AlignLeft();
                            text.Span($"Bill No. : {model.OrderNo}").FontSize(8).Bold();
                        });
                        col.RelativeItem().Text(text =>
                        {
                            text.AlignLeft();
                            text.Span($"Date: {model.DateMiti}").FontSize(8).Bold();
                        });
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.AlignLeft();
                            text.Span($"Name: {model.CustomerName}").FontSize(8).Bold();
                        });
                        row.RelativeItem().Text(text =>
                        {
                            text.AlignLeft();
                            text.Span($"Payment: {model.TermsOfPayment}").FontSize(8).Bold();
                        });
                    });
                    column.Item().Text(text =>
                    {
                        text.AlignLeft();
                        text.Span($"PAN: {model.CustomerPan}").FontSize(8).Bold();
                    });

                    if (model.SalesAdditional.Length > 0)
                        column.Item().Text(text =>
                        {
                            text.AlignLeft();
                            text.Span($"Locker Detail: {model.SalesAdditional}").FontSize(8).Bold();
                        });

                    column.Item().Text(text =>
                    {
                        text.AlignLeft();
                        text.Span($"Print Date: {model.PrintDate}").FontSize(8).Bold();
                    });
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            var model = ObjPdfData;
            container.PaddingVertical(5).Column(col =>
            {
                col.Item().Layers(layers =>
                {
                    layers.Layer().AlignCenter().AlignMiddle().PaddingTop(80).Rotate(-15)
                        .Text(model.NoOfCopy).FontSize(10).ExtraBlack().ExtraBlack().FontColor("#9F9F9F");
                    layers.PrimaryLayer().Element(ComposeTable);
                });
                col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                if (isTi)
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(4);
                        row.RelativeItem(3).AlignLeft().Column(column =>
                        {
                            column.Item().Text("Tax Amount:").FontSize(8).Bold();
                        });
                        row.RelativeItem(2).AlignRight().Column(column =>
                        {
                            column.Item().PaddingRight(5).Text($"{model.TaxAmount}").FontSize(8).Bold();
                        });
                    });
                if (model.IsLoyatlyPoint)
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(4);
                        row.RelativeItem(3).AlignLeft().Column(column =>
                        {
                            column.Item().Text("Loyalty Discount:").FontSize(8).Bold();
                        });
                        row.RelativeItem(2).AlignRight().Column(column =>
                        {
                            column.Item().PaddingRight(5).Text($"{model.LoyaltyAmount}").FontSize(8).Bold();
                        });
                    });

                col.Item().Row(row =>
                {
                    row.RelativeItem(4);
                    row.RelativeItem(3).AlignLeft().Column(column =>
                    {
                        column.Item().Text("Total Amount:").FontSize(8).Bold();
                    });
                    row.RelativeItem(2).AlignRight().Column(column =>
                    {
                        column.Item().PaddingRight(5).Text($"{model.GrandTotal}").FontSize(8).Bold();
                    });
                });
                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                col.Item().Text(text =>
                {
                    text.AlignLeft();
                    text.Span($"Rs. {model.TotalAmountInWord} ").FontSize(8).Bold();
                });
                col.Item().Text(text =>
                {
                    text.AlignLeft();
                    text.Span("Conditions Apply").FontSize(8).Underline().Bold();
                });

                col.Item().Text(text =>
                {
                    text.AlignLeft();
                    text.Span("1. Goods once sold will not be returned.").FontSize(7).Bold();
                });
                col.Item().Text(text =>
                {
                    text.AlignLeft();
                    text.Span("2. Exchange within 7 days with receipt except Saturday.").FontSize(7).Bold();
                });
                col.Item().Text(text =>
                {
                    text.AlignCenter();
                    text.Span("THANK YOU !! PLEASE VISIT AGAIN!!").FontSize(8).Bold();
                });
            });
        }

        private void ComposeTable(IContainer container)
        {
            var model = ObjPdfData;

            container.MinHeight(100).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(12);
                    columns.RelativeColumn();
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).AlignLeft().Text("S.N").FontSize(7).Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("HS Code").FontSize(7).Bold();
                    header.Cell().Element(CellStyle).PaddingLeft(2).Text("Particulars").FontSize(7).Bold();
                    header.Cell().Element(CellStyle).AlignLeft().Text("Qty").FontSize(7).Bold();
                    header.Cell().Element(CellStyle).AlignLeft().Text("Rate").FontSize(7).Bold();
                    header.Cell().Element(CellStyle).AlignLeft().Text("Amount").FontSize(7).Bold();

                    IContainer CellStyle(IContainer cellContainer)
                    {
                        return cellContainer.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(0.7f);
                    }
                });
                foreach (var item in model.SalesInvoiceDetail)
                {
                    table.Cell().AlignLeft().Text(item.SlNo.ToString()).FontSize(7).Bold();
                    table.Cell().AlignCenter().Text(item.HsCode).FontSize(6);
                    table.Cell().PaddingLeft(2).Text(item.ProductName).FontSize(7).Bold();
                    table.Cell().AlignLeft().Text(item.Quantity.ToString(CultureInfo.InvariantCulture)).FontSize(7).Bold();
                    table.Cell().AlignLeft()
                        .Text(isTi
                            ? item.Rate.ToString(CultureInfo.InvariantCulture)
                            : (item.Amount / item.Quantity).ToString(CultureInfo.InvariantCulture)).FontSize(7).Bold();
                    table.Cell().AlignLeft().PaddingLeft(1)
                        .Text(isTi
                            ? item.GrossAmount.ToString(CultureInfo.InvariantCulture)
                            : item.Amount.ToString(CultureInfo.InvariantCulture)).FontSize(7).Bold();
                }
            });
        }
    }
}

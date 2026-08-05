using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NextWave.Erp.Sales.Pdf
{

    public class SalesReturnPosPdf(PdfForSalesReturnModel data) : IDocument
    {
        public PdfForSalesReturnModel ObjPdfData { get; } = data;

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
                    if (model.BranchName != null)
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
                            if (!string.IsNullOrWhiteSpace(model.BranchContactNo))
                                column.Item().Text(text =>
                                {
                                    text.AlignCenter();
                                    text.Span($"PHONE:{model.BranchContactNo}").FontSize(8).Bold();
                                });
                            if (!string.IsNullOrWhiteSpace(model.Pan))
                                column.Item().Text(text =>
                                {
                                    text.AlignCenter();
                                    text.Span($"PAN : {model.Pan}").FontSize(8).Bold();
                                });
                            column.Item().Text(text =>
                            {
                                text.AlignCenter();
                                //        if (_isTI)
                                text.Span("CREDIT NOTE").FontSize(8).Bold();
                                //      else
                                //          text.Span("ABBREVIATED TAX INVOICE").FontSize(8);
                            });
                        }

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.AlignLeft();
                            text.Span($"Bill No. : {model.OrderNo}").FontSize(8).Bold();
                        });
                        row.RelativeItem().Text(text =>
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
                            text.Span($"PAN: {model.CustomerPan}").FontSize(8).Bold();
                        });
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
                    //layers.Layer().AlignCenter().AlignMiddle()
                    //    .Text(model.NoOfCopy).FontSize(10).Bold().FontColor("#B0AFAF");
                    layers.PrimaryLayer().Element(ComposeTable);
                });
                col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                //if (_isTI)
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

            container.MinHeight(150).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(12);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).AlignLeft().Text("S.N").FontSize(7).Bold();
                    header.Cell().Element(CellStyle).PaddingLeft(2).Text("Particulars").FontSize(7).Bold();
                    header.Cell().Element(CellStyle).AlignLeft().Text("Qty").FontSize(7).Bold();
                    header.Cell().Element(CellStyle).AlignLeft().Text("Rate").FontSize(7).Bold();
                    header.Cell().Element(CellStyle).AlignLeft().Text("Amount").FontSize(7).Bold();

                    IContainer CellStyle(IContainer cellContainer)
                    {
                        return cellContainer.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(0.7f);
                    }
                });
                foreach (var item in model.SalesReturnDetail)
                {
                    table.Cell().AlignLeft().Text(item.SlNo.ToString()).FontSize(7).Bold();
                    table.Cell().PaddingLeft(2).Text(item.ProductName).FontSize(7).Bold();
                    table.Cell().AlignLeft().Text(item.Quantity).FontSize(7).Bold();
                    table.Cell().AlignLeft().Text(item.Rate.ToString(CultureInfo.InvariantCulture)).FontSize(7).Bold();
                    table.Cell().AlignLeft().PaddingLeft(1).Text(item.Amount.ToString()).FontSize(7).Bold();
                }
            });
        }
    }
}

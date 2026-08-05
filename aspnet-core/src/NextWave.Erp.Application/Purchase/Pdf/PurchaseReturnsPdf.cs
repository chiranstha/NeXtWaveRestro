using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Globalization;

namespace NextWave.Erp.Purchase.Pdf
{

    public class PurchaseReturnsPdf(List<PdfForPurchaseReturnModel> list) : IDocument
    {
        private int _rowcount;
        public List<PdfForPurchaseReturnModel> ObjPdfData { get; } = list;

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
            for (var i = 0; i < ObjPdfData.Count; i++)
            {
                _rowcount = i;
                container
                    .Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.MarginVertical(10);
                        page.MarginHorizontal(5);
                        page.Header().Element(ComposeHeader);
                        page.Content().Element(ComposeContent);
                    });
            }
        }

        private void ComposeHeader(IContainer container)
        {
            container
                .Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Element(Header);
                        column.Item().Element(Header2);
                    });
                });
        }

        private void Header(IContainer container)
        {
            var model = ObjPdfData[_rowcount];
            var titlestyle = TextStyle.Default.FontSize(10);
            container
                .Border(0.7f)
                .Row(row =>
                {
                    if (model.Logo1 != null)
                        if (model.Logo1.Length > 0)
                            row.RelativeItem(2).AlignLeft().Column(column =>
                            {
                                column.Item().AlignLeft().Height(50).Image(model.Logo1).FitArea();
                            });
                    row.RelativeItem(15).AlignCenter().Column(col =>
                    {
                        col.Item().AlignCenter().PaddingVertical(2).Text(model.BranchName).FontSize(18).ExtraBold();
                        col.Item().AlignCenter().PaddingVertical(1).Text(model.BranchAddress).Style(titlestyle);
                        col.Item().AlignCenter().PaddingVertical(1).Text($"Ph No.: {model.BranchContact}").Style(titlestyle);
                        col.Item().AlignCenter().PaddingVertical(1).Text($"PAN : {model.Pan}").Style(titlestyle);
                    });
                    if (model.Logo1 != null)
                        if (model.Logo1.Length > 0)
                            row.RelativeItem(2);
                });
        }

        private void Header2(IContainer container)
        {
            var model = ObjPdfData[_rowcount];
            container
                .Border(0.7f)
                .Row(row =>
                {
                    row.RelativeItem().PaddingVertical(2).Column(column =>
                    {
                        column.Item().Text(text =>
                        {
                            text.DefaultTextStyle(x => x.SemiBold());
                            text.AlignLeft();
                            text.Span(model.DebitOrCreditNote ? "   Credit" : "   Debit");
                            text.Span(" Note No. " + model.OrderNo);
                        });
                    });
                    row.RelativeItem().PaddingVertical(2).Column(column =>
                    {
                        column.Item().Text(text =>
                        {
                            text.DefaultTextStyle(x => x.ExtraBold());
                            if (!model.DebitOrCreditNote)
                                text.Span("DEBIT NOTE").FontSize(11).Bold();
                            else
                                text.Span("CREDIT NOTE").FontSize(11).Bold();
                            text.AlignCenter();
                        });
                    });
                    row.RelativeItem().PaddingVertical(2).Column(column =>
                    {
                        column.Item().PaddingHorizontal(10).Text(text =>
                        {
                            text.DefaultTextStyle(x => x.SemiBold());
                            text.AlignRight();
                            text.Span($"DateMiti: {model.DateMiti}");
                        });
                    });
                });
        }

        private void ComposeContent(IContainer container)
        {
            var model = ObjPdfData[_rowcount];
            container
                .Border(0.7f)
                .Column(column =>
                {
                    column.Item().Element(ComposeCustomerBox);
                    column.Item().Layers(layers =>
                    {
                        layers.PrimaryLayer().Element(ComposeTable);
                    });
                    column.Item().Element(ComposeAmountsInWords);
                    column.Item().Element(ComposeFooter);
                });
        }

        private void ComposeCustomerBox(IContainer container)
        {
            var model = ObjPdfData[_rowcount];
            container
                .Border(0.7f)
                .Row(row =>
                {
                    row.ConstantItem(400).PaddingLeft(5).Column(col =>
                    {
                        col.Item().PaddingVertical(1).Text(text =>
                        {
                            text.Span(" Party Name: ");
                            text.Span(model.CustomerName);
                        });
                        col.Item().PaddingVertical(1).Text(text =>
                        {
                            text.Span(" Party Address: ");
                            text.Span($" {model.CustomerAddress}");
                        });
                        col.Item().PaddingVertical(1).Text(text =>
                        {
                            text.Span(" Party PAN: ");
                            text.Span(model.CustomerPan);
                        });
                    });

                    row.RelativeItem().PaddingLeft(5).Column(col =>
                    {
                        col.Item().PaddingVertical(1).Text(text =>
                        {
                            text.Span(" Purchase Invoice No : ");
                            text.Span(model.PurchaseVoucherNo);
                        });
                    });
                });
        }

        private void ComposeTable(IContainer container)
        {
            var model = ObjPdfData[_rowcount];
            container
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(28);
                        columns.RelativeColumn();
                        columns.RelativeColumn(4);
                        columns.ConstantColumn(50);
                        columns.ConstantColumn(65);
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(85);
                    });
                    table.ExtendLastCellsToTableBottom();
                    table.Header(header =>
                    {
                        header.Cell().BorderRight(0.7f).Element(CellStyle).Text(text =>
                        {
                            text.DefaultTextStyle(x => x.SemiBold());
                            text.AlignCenter();
                            text.Span("S.N");
                        });
                        header.Cell().BorderRight(0.7f).Element(CellStyle).PaddingLeft(10).Text("HSCode");
                        header.Cell().BorderRight(0.7f).Element(CellStyle).PaddingLeft(10).Text("Product Name");
                        header.Cell().BorderRight(0.7f).Element(CellStyle).AlignCenter().Text("Qty");
                        header.Cell().BorderRight(0.7f).Element(CellStyle).AlignCenter().Text("Unit");
                        header.Cell().BorderRight(0.7f).Element(CellStyle).AlignCenter().Text("Rate");
                        header.Cell().Element(CellStyle).AlignCenter().Text("Amount");

                        IContainer CellStyle(IContainer cellContainer)
                        {
                            return cellContainer.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(0.7f);
                        }
                    });

                    foreach (var item in model.PurchaseReturnDetail)
                    {
                        table.Cell().BorderRight(0.7f).Text(text =>
                        {
                            text.DefaultTextStyle(x => x.SemiBold());
                            text.AlignCenter();
                            text.Span(item.SlNo.ToString() + ".");
                        });
                        table.Cell().BorderRight(0.7f).Element(CellStyle).PaddingLeft(5).Text(item.HsCode);
                        table.Cell().BorderRight(0.7f).Element(CellStyle).PaddingLeft(5).Text(item.ProductName);
                        table.Cell().BorderRight(0.7f).Element(CellStyle).AlignLeft().PaddingLeft(5)
                            .Text(item.Quantity.ToString(CultureInfo.InvariantCulture));
                        table.Cell().BorderRight(0.7f).Element(CellStyle).AlignLeft().PaddingLeft(5).Text(item.Unit);
                        table.Cell().BorderRight(0.7f).Element(CellStyle).AlignLeft().PaddingLeft(5)
                            .Text(item.Rate.ToString(CultureInfo.InvariantCulture));
                        table.Cell().AlignLeft().Element(CellStyle).PaddingLeft(5).Text(item.Amount.ToString());

                        IContainer CellStyle(IContainer cellContainer)
                        {
                            return cellContainer.DefaultTextStyle(x => x.Bold().FontSize(12));
                        }
                    }

                    for (var i = 0; i < 29 - model.PurchaseReturnDetail.Count; i++)
                    {
                        table.Cell().BorderRight(0.7f).Text("");
                        table.Cell().BorderRight(0.7f).PaddingLeft(10).Text("");
                        table.Cell().BorderRight(0.7f).PaddingLeft(10).Text("");
                        table.Cell().BorderRight(0.7f).PaddingRight(5).AlignRight().Text("");
                        table.Cell().BorderRight(0.7f).PaddingLeft(5).AlignLeft().Text("");
                        table.Cell().BorderRight(0.7f).PaddingRight(5).AlignRight().Text("");
                        table.Cell().PaddingRight(5).AlignRight().Text("");
                    }

                    table.Footer(footer =>
                    {
                        footer.Cell().ColumnSpan(6).Border(0.7f).PaddingHorizontal(10).PaddingVertical(3).Text(text =>
                        {
                            text.DefaultTextStyle(x => x.SemiBold());
                            text.AlignRight();
                            text.Span("Total");
                        });
                        footer.Cell().Border(0.7f).AlignLeft().PaddingLeft(5).PaddingVertical(3).Text(model.TotalAmount.ToString()).Bold();
                        footer.Cell().RowSpan(4).ColumnSpan(3).Border(0.7f).Column(col =>
                        {
                            col.Item().Text(text =>
                            {
                                text.DefaultTextStyle(x => x.SemiBold());
                                text.AlignLeft();
                                text.Span($"  Description: {model.Description}");
                            });
                        });
                        footer.Cell().ColumnSpan(3).Border(0.7f).Text("     Discount Amount").SemiBold();
                        footer.Cell().Border(0.7f).AlignLeft().PaddingLeft(5).PaddingVertical(1).Text(model.DiscountAmount.ToString()).SemiBold();
                        footer.Cell().ColumnSpan(3).Border(0.7f).Text("     Taxable Amount").SemiBold();
                        footer.Cell().Border(0.7f).AlignLeft().PaddingLeft(5).PaddingVertical(1).Text(model.TaxableAmount.ToString()).SemiBold();
                        footer.Cell().ColumnSpan(3).Border(0.7f).Text("     VAT @13 %").SemiBold();
                        footer.Cell().Border(0.7f).AlignLeft().PaddingLeft(5).PaddingVertical(1).Text(model.TaxAmount.ToString()).SemiBold();
                        footer.Cell().ColumnSpan(3).Border(0.7f).Text("     Grand Total").Bold();
                        footer.Cell().Border(0.7f).AlignLeft().PaddingLeft(5).PaddingVertical(1).Text(model.GrandTotal.ToString()).Bold();
                    });
                });
        }

        private void ComposeAmountsInWords(IContainer container)
        {
            var model = ObjPdfData[_rowcount];
            container
                .Border(0.7f)
                .Row(row => { row.RelativeItem().Text($"  Amount in Words:  {model.TotalAmountInWord}").Bold(); });
        }

        private void ComposeFooter(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().PaddingTop(35).Row(row =>
                {
                    row.RelativeItem().PaddingLeft(10).Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.AlignCenter();
                            text.DefaultTextStyle(x => x.ExtraLight());
                            text.Span(".....................................");
                        });

                        col.Item().Text(text =>
                        {
                            text.AlignCenter();
                            text.DefaultTextStyle(x => x.SemiBold());
                            text.Span("Approved By");
                        });
                    });
                    row.RelativeItem();
                    row.RelativeItem().PaddingLeft(10).Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.AlignCenter();
                            text.DefaultTextStyle(x => x.ExtraLight());
                            text.Span(".....................................");
                        });
                        col.Item().Text(text =>
                        {
                            text.AlignCenter();
                            text.DefaultTextStyle(x => x.SemiBold());
                            text.Span("Received By");
                        });
                    });
                });
            });
        }
    }
}

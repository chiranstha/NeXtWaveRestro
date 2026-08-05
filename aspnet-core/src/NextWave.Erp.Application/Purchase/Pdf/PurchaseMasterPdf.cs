using NextWave.Erp.Purchase.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Globalization;

namespace NextWave.Erp.Purchase.Pdf
{

    public class PurchaseMasterPdf(List<PdfForPurchaseMasterModel> list) : IDocument
    {
        //private readonly int _rowcount = 0;
        //public List<PdfForPurchaseMasterModel> ObjPdfData { get; } = list;

        //public DocumentMetadata GetMetadata()
        //{
        //    return DocumentMetadata.Default;
        //}

        //public DocumentSettings GetSettings()
        //{
        //    return DocumentSettings.Default;
        //}

        //public void Compose(IDocumentContainer container)
        //{
        //    container
        //        .Page(page =>
        //        {
        //            page.MarginHorizontal(10);
        //            page.MarginVertical(10);
        //            page.Header().Element(ComposeHeader);
        //            page.Content().Element(ComposeContent);
        //        });
        //}

        //private void ComposeHeader(IContainer container)
        //{
        //    var model = ObjPdfData[_rowcount];
        //    container.Row(row =>
        //    {
        //        if (model.Logo1 != null)
        //            row.RelativeItem().Column(column =>
        //            {
        //                column.Item().AlignLeft().Height(50).Image(model.Logo1).FitArea();
        //            });

        //        row.RelativeItem(4).Column(column =>
        //        {
        //            column.Item().AlignCenter().Text($"{model.BranchName}").FontColor("#04063b").FontSize(16).Bold();
        //            column.Item().AlignCenter().Text($"{model.Address} ").FontSize(12);
        //            column.Item().AlignCenter().Text($"{model.BranchPhone} ").FontSize(12);
        //            column.Item().AlignCenter().PaddingTop(10).Text($"PAN: {model.BranchPan} ").FontSize(14)
        //                .FontColor("#04063b").Medium();
        //        });
        //    });
        //}

        //private void ComposeContent(IContainer container)
        //{
        //    var model = ObjPdfData[_rowcount];
        //    container.PaddingVertical(20).Column(col =>
        //    {
        //        col.Item().BorderTop(1).BorderBottom(1).Padding(5).Row(row =>
        //        {
        //            row.RelativeItem().Text($"Vendor Invoice No. : {model.VendorInvoiceNo}").FontSize(10);

        //            row.RelativeItem().Text(text =>
        //            {
        //                text.AlignCenter();
        //                text.Span("PURCHASE INVOICE").FontSize(10).Bold();
        //            });

        //            row.RelativeItem().Text(text =>
        //            {
        //                text.AlignRight();
        //                text.Span($"Date : {model.DateMiti}").FontSize(10);
        //            });
        //        });

        //        col.Item().PaddingTop(10).Row(row =>
        //        {
        //            row.RelativeItem().Column(column =>
        //            {
        //                column.Item().Padding(2)
        //                    .Text($"Voucher No: {model.OrderNo}Party: {model.LedgerName}").FontSize(11)
        //                    .SemiBold();
        //            });
        //        });

        //        col.Item().Element(ComposeTable);

        //        col.Item().PaddingBottom(5).Text($"Amount In Words : {model.TotalAmountInWord}").FontSize(10).Medium();
        //        col.Item().Row(row =>
        //        {
        //            row.RelativeItem(7).Border(1).BorderColor("CCC").AlignLeft().Column(column =>
        //            {
        //                column.Item().Background("EEE").Padding(5).Text("NARRATION : ").FontSize(11).Medium();
        //                column.Item().PaddingLeft(10).Padding(5).Text($"{model.Description}");
        //            });
        //            row.RelativeItem();
        //            row.RelativeItem(2).AlignRight().Column(column =>
        //            {
        //                column.Item().Background("EEE").Padding(1).Text("Total Amount").FontSize(11).Medium();
        //                column.Item().Padding(1).Text("Discount").FontSize(11).Medium();
        //                column.Item().Padding(1).Text("Taxable Amount").FontSize(11).Medium();
        //                column.Item().Background("EEE").Padding(1).Text("VAT @ 13 % ").FontSize(11).Medium();
        //                column.Item().Padding(1).Text("Grand Total").FontSize(11).Medium();
        //            });
        //            row.RelativeItem(2).AlignLeft().Column(column =>
        //            {
        //                column.Item().Background("EEE").Padding(1).Text($": RS. {model.TotalAmount}").FontSize(11);
        //                column.Item().Padding(1).Text($": Rs. {model.BillDiscount}").FontSize(11);
        //                column.Item().Padding(1).Text($": Rs. {model.TaxableAmount}").FontSize(11);
        //                column.Item().Background("EEE").Padding(1).Text($": Rs. {model.VatAmount}").FontSize(11);
        //                column.Item().Padding(1).Text($": Rs. {model.GrandTotal}").FontSize(11);
        //            });
        //        });
        //        col.Item().Element(ComposeComments);
        //    });
        //}

        //private void ComposeTable(IContainer container)
        //{
        //    var model = ObjPdfData[_rowcount];
        //    var headerStyle = TextStyle.Default.FontSize(11).LineHeight(0.5f);

        //    container.Height(450).PaddingTop(10).Decoration(section =>
        //    {
        //        // header
        //        section.Before().BorderBottom(1).Background("EEE").Padding(8).Row(row =>
        //        {
        //            row.ConstantItem(25).AlignLeft().Text("S.N.").Style(headerStyle);
        //            row.RelativeItem().AlignCenter().Text("HSCode").Style(headerStyle);
        //            row.RelativeItem(4).PaddingLeft(10).Text("Product Name").Style(headerStyle);
        //            row.RelativeItem().AlignCenter().Text("Qty").Style(headerStyle);
        //            row.RelativeItem().AlignCenter().Text("Unit").Style(headerStyle);
        //            row.RelativeItem().AlignCenter().Text("Rate").Style(headerStyle);
        //            row.RelativeItem().AlignCenter().Text("Amount").Style(headerStyle);
        //        });

        //        // content
        //        section.Content().Column(column =>
        //        {
        //            foreach (var item in model.PurchaseDetail)
        //                column.Item().BorderBottom(1).BorderColor("CCC").Padding(5).Row(row =>
        //                {
        //                    row.ConstantItem(25).AlignCenter().Text(item.SlNo + ".").FontSize(10);
        //                    row.RelativeItem().AlignCenter().Text(item.HsCode).FontSize(10);
        //                    row.RelativeItem(4).PaddingLeft(10).Text(item.ProductName).FontSize(10);
        //                    row.RelativeItem().AlignCenter().Text(item.Quantity.ToString(CultureInfo.InvariantCulture))
        //                        .FontSize(10);
        //                    row.RelativeItem().AlignCenter().Text(item.Unit).FontSize(10);
        //                    row.RelativeItem().AlignCenter().Text(item.Rate.ToString(CultureInfo.InvariantCulture))
        //                        .FontSize(10);
        //                    row.RelativeItem().AlignCenter().Text(item.GrossAmount.ToString(CultureInfo.InvariantCulture))
        //                        .FontSize(10);
        //                });
        //        });
        //    });
        //}

        //private void ComposeComments(IContainer container)
        //{
        //    container.Extend().AlignBottom().ShowEntire().PaddingTop(40).Row(row =>
        //    {
        //        row.RelativeItem().Column(column =>
        //        {
        //            column.Item().BorderTop(1).Text(text =>
        //            {
        //                text.AlignCenter();
        //                text.Span("Approved By").FontSize(12).Medium();
        //            });
        //        });
        //        row.RelativeItem(2);
        //        row.RelativeItem().Column(column =>
        //        {
        //            column.Item().BorderTop(1).Text(text =>
        //            {
        //                text.AlignCenter();
        //                text.Span("Received By").FontSize(12).Medium();
        //            });
        //        });
        //    });
        //}

        private int _rowcount;
        public List<PdfForPurchaseMasterModel> ObjPdfData { get; } = list;

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
                        col.Item().AlignCenter().PaddingVertical(1).Text(model.Address).Style(titlestyle);
                        col.Item().AlignCenter().PaddingVertical(1).Text($"Ph No.: {model.BranchPhone}").Style(titlestyle);
                        //if (!string.IsNullOrWhiteSpace(model.CompanyEmail))
                        //    col.Item().AlignCenter().Text($"Email : {model.CompanyEmail}").Style(titlestyle);
                        col.Item().AlignCenter().PaddingVertical(1).Text($"PAN : {model.BranchPan}").Style(titlestyle);
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
                            text.Span($"  Voucher No: {model.OrderNo}");
                        });
                    });
                    row.RelativeItem().PaddingVertical(2).Column(column =>
                    {
                        column.Item().Text(text =>
                        {
                            text.DefaultTextStyle(x => x.ExtraBold());
                            text.Span("Purchase Invoice");
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
                            text.Span(model.LedgerName);
                        });
                        col.Item().PaddingVertical(1).Text(text =>
                        {
                            text.Span(" Party Address: ");
                            text.Span($" {model.Address}");
                        });
                        col.Item().PaddingVertical(1).Text(text =>
                        {
                            text.Span(" Party PAN: ");
                            text.Span(model.Pan);
                        });
                    });

                    row.RelativeItem().PaddingLeft(5).Column(col =>
                    {
                        col.Item().PaddingVertical(1).Text(text =>
                        {
                            text.Span(" Vendor Invoice No : ");
                            text.Span(model.VendorInvoiceNo);
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

                    foreach (var item in model.PurchaseDetail)
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
                        table.Cell().AlignLeft().Element(CellStyle).PaddingLeft(5).Text(item.GrossAmount.ToString());

                        IContainer CellStyle(IContainer cellContainer)
                        {
                            return cellContainer.DefaultTextStyle(x => x.Bold().FontSize(12));
                        }
                    }

                    for (var i = 0; i < 33 - model.PurchaseDetail.Count; i++)
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
                        footer.Cell().Border(0.7f).AlignLeft().PaddingLeft(5).PaddingVertical(1).Text(model.BillDiscount.ToString()).SemiBold();
                        footer.Cell().ColumnSpan(3).Border(0.7f).Text("     Taxable Amount").SemiBold();
                        footer.Cell().Border(0.7f).AlignLeft().PaddingLeft(5).PaddingVertical(1).Text(model.TaxableAmount.ToString()).SemiBold();
                        footer.Cell().ColumnSpan(3).Border(0.7f).Text("     VAT @13 %").SemiBold();
                        footer.Cell().Border(0.7f).AlignLeft().PaddingLeft(5).PaddingVertical(1).Text(model.VatAmount.ToString()).SemiBold();
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

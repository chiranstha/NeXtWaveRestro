using System;
using System.Collections.Generic;
using NepDate;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace NextWave.Erp.Sales.Pdf
{

    public class SalesReturnPartyPdf(List<PdfForSalesReturnModel> list) : IDocument
    {
        private readonly int _rowcount = 0;
        public List<PdfForSalesReturnModel> ObjPdfData { get; } = list;

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
                    page.MarginHorizontal(20);
                    page.MarginVertical(20);
                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                });
        }

        private void ComposeHeader(IContainer container)
        {
            var model = ObjPdfData[_rowcount];
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    if (model.Logo1 != null) column.Item().AlignLeft().Height(50).Image(model.Logo1).FitArea();
                });
                if (model.BranchName.Trim().Length > 0)
                    row.RelativeItem(4).Column(column =>
                    {
                        column.Item().AlignCenter().Text($"{model.BranchName}").FontSize(14).Bold();
                        column.Item().AlignCenter().Text($"{model.Address} ").FontSize(12);
                        column.Item().AlignCenter().Text($"{model.BranchContactNo}").FontSize(12);
                        column.Item().AlignCenter().PaddingTop(5).Text($"PAN:{model.Pan}").FontSize(14).Medium();
                    });

                row.RelativeItem().Column(column =>
                {
                    if (model.Logo2 != null) column.Item().AlignLeft().Height(50).Image(model.Logo2).FitArea();
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            var model = ObjPdfData[_rowcount];
            container.PaddingVertical(5).Column(col =>
            {
                //column.Spacing(5);
                col.Item().BorderTop(1).BorderBottom(1).Padding(5).Row(row =>
                {
                    row.RelativeItem().Text($"VoucherNo:- {model.OrderNo}").FontSize(10);

                    row.RelativeItem().Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("CREDIT NOTE").FontSize(10).Bold();
                    });

                    row.RelativeItem().Text(text =>
                    {
                        text.AlignRight();
                        text.Span($"Date : {model.DateMiti}").FontSize(10);
                    });
                });

                col.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem(4).Column(column =>
                    {
                        column.Item().Padding(2).Text($"Customer Name : {model.CustomerName}{model.CustomerAddress}")
                            .FontSize(11);
                        column.Item().Padding(2).Text($"VAT No. : {model.CustomerPan}").FontSize(11);
                    });
                    row.RelativeItem(4).Column(column =>
                    {
                        column.Item().Padding(2)
                            .Text($"Printing Date & Time : {DateTime.Now.ToNepaliDate().ToString()}").FontSize(11);
                    });
                });

                col.Item().Element(ComposeTable);
                col.Item().PaddingBottom(15).Background("EEE").Row(row =>
                {
                    row.ConstantItem(50);
                    row.RelativeItem(3).AlignCenter().Text("SubTotal:-");
                    row.RelativeItem().Text("");
                    row.RelativeItem().Text("");
                    row.RelativeItem().Text("");
                    row.RelativeItem().Text("");
                    row.RelativeItem().AlignCenter().Text($"{model.TotalAmount}").FontSize(10).Thin();
                });
                col.Item().PaddingBottom(5).Text($"Amount In Words : {model.TotalAmountInWord} Only.").FontSize(10)
                    .Medium();
                col.Item().Row(row =>
                {
                    row.RelativeItem(7).Border(1).BorderColor("CCC").AlignLeft().Column(column =>
                    {
                        column.Item().Background("EEE").Padding(5).Text("NARRATION : ").FontSize(11).Medium();
                        column.Item().PaddingLeft(10).Padding(5).Text("");
                    });
                    row.RelativeItem();
                    row.RelativeItem(4).AlignRight().Column(column =>
                    {
                        column.Item().Padding(5).Text("Total Amount :").FontSize(11).Medium();
                        column.Item().Padding(5).Text("Vat Amount :").FontSize(11).Medium();
                        column.Item().Padding(5).Text("Grand Total :").FontSize(11).Medium();
                    });
                    row.RelativeItem(2).AlignLeft().Column(column =>
                    {
                        column.Item().Padding(5).Text($" {model.TotalAmount}").FontSize(11);
                        column.Item().Padding(5).Text($" {model.TaxAmount}").FontSize(11);
                        column.Item().Padding(5).Text($" {model.GrandTotal}").FontSize(11);
                    });
                });
                col.Item().Element(ComposeComments);
            });
        }

        private void ComposeTable(IContainer container)
        {
            var model = ObjPdfData[_rowcount];
            var headerStyle = TextStyle.Default.FontSize(11).LineHeight(0.5f);

            container.PaddingTop(10).Height(340).PaddingBottom(5).Decoration(section =>
            {
                section.Before().BorderBottom(1).Background("EEE").Padding(8).Row(row =>
                {
                    row.ConstantItem(25).AlignLeft().Text("S.N.").Style(headerStyle);
                    row.RelativeItem().PaddingLeft(10).Text("Return Amount").Style(headerStyle);
                    row.RelativeItem().AlignCenter().Text("Return Tax").Style(headerStyle);
                    row.RelativeItem().AlignCenter().Text("Return Tax Amount").Style(headerStyle);
                    row.RelativeItem().AlignCenter().Text("Total Amount").Style(headerStyle);
                });

                section.Content().Column(column =>
                {
                    column.Item().BorderBottom(1).BorderColor("CCC").Padding(5).Row(row =>
                    {
                        var index = 0;
                        row.ConstantItem(25).AlignCenter().Text((++index).ToString()).FontSize(10);
                        row.RelativeItem().PaddingLeft(10).Text(model.NetAmount.ToString()).FontSize(10);
                        row.RelativeItem().AlignCenter().Text("VAT 13%").FontSize(10);
                        row.RelativeItem().AlignCenter().Text(model.TaxAmount.ToString()).FontSize(10);
                        row.RelativeItem().AlignCenter().Text(model.GrandTotal.ToString()).FontSize(10);
                    });
                });
            });
        }

        private void ComposeComments(IContainer container)
        {
            container.Extend().AlignBottom().PaddingTop(40).Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().BorderTop(1).Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("Prepared By ").FontSize(12).Bold();
                    });
                });
                row.ConstantItem(50);
                row.RelativeItem().Column(column => { });
                row.ConstantItem(50);
                row.RelativeItem().Column(column =>
                {
                    column.Item().BorderTop(1).Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("Authorized By").FontSize(12).Bold();
                    });
                });
            });
        }
    }
}

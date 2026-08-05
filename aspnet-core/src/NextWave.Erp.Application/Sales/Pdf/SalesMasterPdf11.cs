using System.Collections.Generic;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


namespace NextWave.Erp.Sales.Pdf
{

    public class SalesMasterPdf11(List<PdfForSalesInvoiceModel> list) : IDocument
    {
        private int _rowcount;
        public List<PdfForSalesInvoiceModel> ObjPdfData { get; } = list;

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
                        page.MarginHorizontal(8);
                        page.MarginVertical(8);
                        page.Header().Element(ComposeHeader);
                        page.Content().Element(ComposeContent);
                        //page.Footer().Element(ComposeComments);
                    });
            }
        }

        private void ComposeHeader(IContainer container)
        {
            var model = ObjPdfData[_rowcount];
            container.Column(col =>
            {
                col.Item().AlignMiddle().Row(row =>
                {
                    if (model.Logo1 != null)
                        if (model.Logo1.Length > 0)
                            row.RelativeItem().AlignCenter().AlignMiddle().Column(column =>
                            {
                                column.Item().Height(50).Image(model.Logo1).FitArea();
                            });

                    if (model.BranchName != null)
                        if (model.BranchName.Trim().Length > 0)
                            row.RelativeItem(3).AlignLeft().Column(column =>
                            {
                                column.Item().Text($"{model.BranchName}").FontSize(14).Bold();
                                if (model.Address != null)
                                    if (model.Address.Trim().Length > 0)
                                        column.Item().Text($"{model.Address}").FontSize(10);
                                if (model.BranchName != null)
                                    if (model.BranchPhone.Trim().Length > 0)
                                        column.Item().Text($"Contact {model.BranchPhone}").FontSize(10);

                                if (model.BranchPan != null)
                                    if (model.BranchPan.Trim().Length > 0)
                                        column.Item().Text($"VAT NO : {model.BranchPan}").FontSize(10);
                                var webSite = model.CompanyEmail.Length > 0 ? $"Email:- {model.CompanyEmail}" : "";
                                if (webSite != "")
                                    column.Item().Text(webSite).FontSize(10);
                            });

                    row.RelativeItem(5).AlignRight().Text(model.InvoiceName).FontSize(20).Bold();
                });
                col.Item().BorderBottom(1).BorderColor("#3188bd").PaddingTop(10).Row(row =>
                {
                    row.RelativeItem(2).AlignBottom().Text("INVOICE TO :").FontSize(14).FontColor("#3188bd");
                });
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem(4).Column(column =>
                    {
                        column.Item().Text($"Party Name : {model.CustomerName}").FontSize(10).Bold();
                        column.Item().Text($"Address : {model.CustomerAddress}").FontSize(10);
                        column.Item().Text($"Phone. : {model.CustomerPhone}").FontSize(10);
                        column.Item().Text($"Party PAN : {model.CustomerPan}").FontSize(10);
                    });

                    row.RelativeItem(2).Column(column =>
                    {
                        column.Item().Text($"Invoice No : {model.OrderNo}").FontSize(10);
                        column.Item().Text($"Invoice Date : {model.DateMiti}").FontSize(10);
                        column.Item().Text($"Print Date: {model.PrintDate}").FontSize(8);
                    });
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            var model = ObjPdfData[_rowcount];
            container.PaddingVertical(10).Column(column =>
            {
                column.Item().Layers(layers =>
                {
                    layers.Layer().AlignCenter().AlignMiddle()
                        .Text(model.NoOfCopy).FontSize(15).Bold().FontColor(Colors.Grey.Lighten2);
                    //layers.Layer().AlignCenter().AlignMiddle().Height(100).Width(100).Column(column =>
                    //{
                    //    using (var client = new WebClient())
                    //    {
                    //        var bytes = client.DownloadData(
                    //            "https://e7.pngegg.com/pngimages/46/626/png-clipart-c-logo-the-c-programming-language-computer-icons-computer-programming-source-code-programming-miscellaneous-template-thumbnail.png");

                    //        column.Item().Height(50).Image(bytes, ImageScaling.FitArea);
                    //    }
                    //});
                    layers.PrimaryLayer().Element(ComposeTable);
                });
                column.Item().Element(ComposeComments);
                //column.Item().PaddingTop(25).Element(ComposeComments);
            });
        }

        private void ComposeTable(IContainer container)
        {
            var model = ObjPdfData[_rowcount];
            var headerStyle = TextStyle.Default
                .FontSize(11)
                .LineHeight(0.5f);

            var contentStyle = TextStyle.Default
                .FontSize(10);


            container.MinHeight(400).Decoration(section =>
            {
                // header
                section.Before().AlignMiddle().BorderBottom(1).BorderColor("#3188bd").Background("DDD").Padding(8).Row(
                    row =>
                    {
                        row.ConstantItem(25).AlignCenter().Text("S.N.").Style(headerStyle);
                        row.RelativeItem().AlignCenter().Text("HSCode").Style(headerStyle);
                        row.RelativeItem(4).PaddingLeft(10).Text("Item Description").Style(headerStyle);

                        row.RelativeItem().AlignCenter().Text("Quantity").Style(headerStyle);
                        row.RelativeItem().AlignCenter().Text("Unit").Style(headerStyle);
                        row.RelativeItem().AlignCenter().Text("Rate").Style(headerStyle);

                        row.RelativeItem().AlignCenter().Text("Amount").Style(headerStyle);
                    });

                // content
                section
                    .Content()
                    .Column(column =>
                    {
                        foreach (var item in model.SalesInvoiceDetail)
                            column.Item().BorderBottom(1).BorderColor("CCC").Padding(3).Row(row =>
                            {
                                row.ConstantItem(30).AlignCenter().Text(item.SlNo.ToString());
                                row.RelativeItem().AlignCenter().Text(item.HsCode).Style(contentStyle);
                                row.RelativeItem(4).PaddingLeft(10).Text(item.ProductName).Style(contentStyle);
                                row.RelativeItem().AlignCenter().Text(item.Quantity.ToString(CultureInfo.InvariantCulture))
                                    .Style(contentStyle);
                                row.RelativeItem().AlignCenter().Text(item.Unit).Style(contentStyle);
                                row.RelativeItem().AlignCenter().Text(item.Rate.ToString(CultureInfo.InvariantCulture))
                                    .Style(contentStyle);

                                row.RelativeItem().AlignCenter().Text(item.Amount.ToString()).Style(contentStyle);
                            });
                    });
                //section.Footer().Row(row =>
                //{
                //    row.ConstantItem(30).Text("").Style(headerStyle);
                //    row.RelativeItem(2).Text("").Style(headerStyle);
                //    row.RelativeItem().Background("#3188bd").Padding(5).AlignCenter().Text("Grand Total").Style(headerStyle);
                //    row.RelativeItem().Background("#3188bd").Padding(5).AlignCenter().Text("Rs. 2154200").Style(headerStyle);
                //});
            });
        }

        private void ComposeComments(IContainer container)
        {
            var model = ObjPdfData[_rowcount];
            container.Extend().AlignBottom().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem(6).Border(1).BorderColor("CCC").AlignLeft().Column(column =>
                    {
                        column.Item().Background("EEE").Padding(5).Text("NARRATION : ").FontSize(11).Medium();
                        column.Item().PaddingLeft(10).Padding(5).Text($"{model.Description}");
                    });

                    row.RelativeItem(3).AlignRight().Column(column =>
                    {
                        column.Item().Background("EEE").Padding(1).PaddingLeft(5).Text("Total Amount").Medium()
                            .FontSize(12);
                        column.Item().Padding(1).PaddingLeft(5).Text("Discount").Medium().FontSize(12);
                        column.Item().Background("EEE").Padding(1).PaddingLeft(5).Text("VAT @ 13 %").Medium().FontSize(12);
                        column.Item().Padding(1).PaddingLeft(5).Text("Taxable Amount").Medium().FontSize(12);
                        column.Item().Background("#3188bd").Padding(1).PaddingLeft(5).Text("Net Amount").Medium()
                            .FontColor("#ffffff").FontSize(12);
                    });
                    row.RelativeItem(2).Column(column =>
                    {
                        column.Item().Background("EEE").Padding(1).Text($": RS.{model.TotalAmount}").FontSize(12);
                        column.Item().Padding(1).Text($": Rs. {model.DiscountAmount}").FontSize(12);
                        column.Item().Background("EEE").Padding(1).Text($": Rs.  {model.TaxAmount}").FontSize(12);
                        column.Item().Padding(1).Text($": Rs. {model.TaxableAmount}").FontSize(12);
                        column.Item().Background("#3188bd").Padding(1).Text($": Rs. {model.GrandTotal}")
                            .FontColor("#ffffff").FontSize(12);
                    });
                });

                col.Item().PaddingTop(5).Border(1).BorderColor("CCC").Row(row =>
                {
                    row.RelativeItem(7).Padding(10).Column(column =>
                    {
                        column.Item().Padding(1).Text($"TRANSPOTER NAME : {model.TransportName}").FontSize(10).Medium();
                        column.Item().Padding(1).Text($"VEHICLE NO : {model.VehicleNumber}").FontSize(10).Medium();
                    });

                    row.RelativeItem(4).Padding(10).Column(column =>
                    {
                        column.Item().Padding(1).Text($"FRIGHT TERMS : {model.FrightTerms}").Medium().FontSize(10);
                        column.Item().Padding(1).Text($"FRIGHT : {model.Fright}").Medium().FontSize(10);
                    });
                });

                col.Item().PaddingTop(45).Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().BorderTop(1).Text(text =>
                        {
                            text.AlignCenter();
                            text.Span("Receiver Signature ").FontSize(12).Bold();
                        });
                    });
                    row.ConstantItem(50);
                    row.RelativeItem().Column(column =>
                    {
                        //column.Item().BorderTop(1)
                        //    .Text("Checked By", TextStyle.Default.FontSize(12).Bold().AlignCenter());
                    });
                    row.ConstantItem(50);
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().BorderTop(1).Text(text =>
                        {
                            text.AlignCenter();
                            text.Span("Authorised  Signature").FontSize(12).Bold();
                        });
                    });
                });

                col.Item().PaddingTop(15).AlignLeft().Text(text =>
                {
                    text.AlignCenter();
                    text.Span("Terms & Conditions : Goods once sold and taken out of permises wont be return back other than damaged one.").FontSize(9);
                });
            });
        }
    }
}

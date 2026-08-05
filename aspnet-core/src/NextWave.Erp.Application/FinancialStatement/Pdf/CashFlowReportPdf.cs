using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace NextWave.Erp.FinancialStatement.Pdf;

public class CashFlowReportPdf(CashFlowPdfDto model) : IDocument
{
    public CashFlowPdfDto Model { get; } = model;

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
                page.MarginHorizontal(15);
                page.MarginVertical(15);
                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                //page.Footer().Element(ComposeComments);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        //var model = objPdfData[rowcount];
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                //if (Model.Image1 != null)
                //{
                //    row.RelativeItem().AlignCenter().AlignMiddle().Column(column =>
                //    {

                //        column.Item().Height(40).Image(Model.Image1, ImageScaling.FitArea);


                //    });
                //}
                row.RelativeItem(4).AlignLeft().Column(column =>
                {
                    column.Item().AlignLeft().Text($"{Model.CompanyInfo.Name}").FontSize(18).Bold();
                    column.Item().AlignLeft().Text($"{Model.CompanyInfo.Address}").FontSize(10);
                    column.Item().AlignLeft().Text($"Contact: {Model.CompanyInfo.CompanyContact}").FontSize(10);
                });
                row.ConstantItem(20);
                row.RelativeItem(4).AlignRight().Column(column =>
                {
                    column.Item().AlignRight().Text("Cash Flow Report").FontSize(18).Bold();
                    if (!string.IsNullOrWhiteSpace(Model.ReportParameters?.StartDate) ||
                        !string.IsNullOrWhiteSpace(Model.ReportParameters?.EndDate))
                        column.Item().AlignRight()
                            .Text($"{Model.ReportParameters?.StartDate} - {Model.ReportParameters?.EndDate}")
                            .FontSize(10);
                    if (!string.IsNullOrWhiteSpace(Model.ReportParameters?.ComparisonPeriod))
                        column.Item().AlignRight().Text($"Comparison: {Model.ReportParameters.ComparisonPeriod}")
                            .FontSize(10);
                });
                row.ConstantItem(20);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Item().Element(ComposeTable1);
            column.Item().Element(ComposeComments);
        });
    }

    private void ComposeTable1(IContainer container)
    {
        var headerStyle = TextStyle.Default.Medium().FontSize(12).ExtraBold();
        var contentStyle = TextStyle.Default
            .FontSize(9)
            .Thin();

        container.Decoration(decoration =>
        {
            // header
            decoration.Before().Background("EEE").BorderBottom(1).Row(row =>
            {
                row.RelativeItem(3).AlignLeft().Text("Name").Style(headerStyle);
                row.RelativeItem().AlignRight().Text("Current Period").Style(headerStyle);
                row.RelativeItem().AlignRight().Text("Previous Period").Style(headerStyle);
                row.RelativeItem().AlignLeft().Text("Type").Style(headerStyle);
            });
            // content
            decoration.Content().MinHeight(300).Column(column =>
            {
                foreach (var item in Model.CashFlowDetails ?? [])
                    column.Item().BorderBottom(1).BorderColor("EEE").Row(row =>
                    {
                        var nameStyle = item.Level == 0 ? contentStyle.Bold() : contentStyle;
                        row.RelativeItem(3).PaddingLeft(item.Level * 8).AlignLeft()
                            .Text($"{item.Name?.TrimStart()}").Style(nameStyle);
                        row.RelativeItem().Padding(2).AlignRight().Text($"{item.CurrentAmount:N2}").Style(contentStyle);
                        row.RelativeItem().Padding(2).AlignRight().Text($"{item.PreviousAmount:N2}").Style(contentStyle);
                        row.RelativeItem().Padding(2).AlignLeft().Text($"{item.GroupType}").Style(contentStyle);
                    });
            });
        });
    }

    private void ComposeTable2(IContainer container)
    {
        var headerStyle = TextStyle.Default.Medium().FontSize(12).ExtraBold();
        var contentStyle = TextStyle.Default
            .FontSize(9)
            .Thin();

        container.Decoration(decoration =>
        {
            // header
            decoration.Before().Background("EEE").BorderBottom(1).Row(row =>
            {
                //row.RelativeItem(3).AlignLeft().Text("Date").Style(headerStyle);
                row.RelativeItem(3).AlignLeft().Text("Name").Style(headerStyle);
                row.RelativeItem().AlignLeft().Text("Debit").Style(headerStyle);
                row.RelativeItem().AlignLeft().Text("Credit").Style(headerStyle);
                row.RelativeItem().AlignLeft().Text("Debit %").Style(headerStyle);
                row.RelativeItem().AlignLeft().Text("Credit %").Style(headerStyle);
            });
            // content
            decoration.Content().MinHeight(300).Column(column =>
            {
                //foreach (var item in Model.InFlowCash)
                //    column.Item().BorderBottom(1).BorderColor("EEE").Row(row =>
                //    {
                //        row.RelativeItem(3).AlignLeft().Text($"{item.Name}").Style(contentStyle);
                //        row.RelativeItem().Padding(2).AlignLeft().Text($"{item.Debit}").Style(contentStyle);
                //        row.RelativeItem().Padding(2).AlignLeft().Text($"{item.Credit}").Style(contentStyle);
                //        row.RelativeItem().Padding(2).AlignLeft().Text($"{item.DebitPercentage}").Style(contentStyle);
                //        row.RelativeItem().Padding(2).AlignLeft().Text($"{item.CreditPercentage}").Style(contentStyle);
                //    });
            });
        });
    }

    private void ComposeComments(IContainer container)
    {
        container.Extend().AlignBottom().Column(col =>
        {
            col.Item().PaddingTop(40).Row(row =>
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
                        text.Span("Authorized By").FontSize(12).Bold();
                    });
                });
            });
        });
    }
}

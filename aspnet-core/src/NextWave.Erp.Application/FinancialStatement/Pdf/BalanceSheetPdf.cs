using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using NextWave.Erp.FinancialStatement.Pdf.Dto;

namespace NextWave.Erp.FinancialStatement.Pdf;

public class BalanceSheetPdf(BalanceSheetPdfDto model) : IDocument
{
    public BalanceSheetPdfDto Model { get; } = model;

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
            });
    }

    private void ComposeHeader(IContainer container)
    {
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
                    column.Item().AlignLeft().Text($"Contact: {Model.CompanyInfo.PhoneNo1}").FontSize(10);
                });
                row.ConstantItem(20);
                row.RelativeItem(4).AlignRight().Column(column =>
                {
                    column.Item().AlignRight().Text("Balance Sheet Report").FontSize(18).Bold();
                    if (!string.IsNullOrWhiteSpace(Model.FromMiti) || !string.IsNullOrWhiteSpace(Model.ToMiti))
                        column.Item().AlignRight().Text($"{Model.FromMiti} - {Model.ToMiti}").FontSize(10);
                });
                row.ConstantItem(20);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Item().Element(ComposeTable);
            column.Item().Element(ComposeComments);
        });
    }

    private void ComposeTable(IContainer container)
    {
        var headerStyle = TextStyle.Default.Medium().FontSize(12).Bold();
        var contentStyle = TextStyle.Default.FontSize(9);

        container.Decoration(decoration =>
        {
            // header
            decoration.Before().Background("CCC").BorderBottom(1).Row(row =>
            {
                row.RelativeItem(3).AlignLeft().Text("Name").Style(headerStyle);
                row.RelativeItem().AlignLeft().Text("OpeningDr").Style(headerStyle);
                row.RelativeItem().AlignLeft().Text("OpeningCr").Style(headerStyle);
                row.RelativeItem().AlignLeft().Text("Debit").Style(headerStyle);
                row.RelativeItem().AlignLeft().Text("Credit").Style(headerStyle);
                row.RelativeItem().AlignLeft().Text("ClosingDr").Style(headerStyle);
                row.RelativeItem().AlignLeft().Text("ClosingCr").Style(headerStyle);
            });
            // content
            decoration.Content().MinHeight(550).Column(column =>
            {
                foreach (var item in Model.BalanceSheetDetails)
                    column.Item().BorderBottom(1).BorderColor("DDD").Row(row =>
                    {
                        row.RelativeItem(3).AlignLeft().Text($"{item.Name}").Style(contentStyle);
                        row.RelativeItem().Padding(2).AlignLeft().Text($"{item.OpeningDr}").Style(contentStyle);
                        row.RelativeItem().Padding(2).AlignLeft().Text($"{item.OpeningCr}").Style(contentStyle);
                        row.RelativeItem().Padding(2).AlignLeft().Text($"{item.Debit}").Style(contentStyle);
                        row.RelativeItem().Padding(2).AlignLeft().Text($"{item.Credit}").Style(contentStyle);
                        row.RelativeItem().Padding(2).AlignLeft().Text($"{item.ClosingDr}").Style(contentStyle);
                        row.RelativeItem().Padding(2).AlignLeft().Text($"{item.ClosingCr}").Style(contentStyle);
                    });
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
        });
    }
}

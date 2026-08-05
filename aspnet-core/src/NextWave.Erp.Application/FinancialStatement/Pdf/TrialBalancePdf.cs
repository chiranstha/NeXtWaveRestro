using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using NextWave.Erp.FinancialStatement.Pdf.Dto;
using NextWave.Erp.Enums;

namespace NextWave.Erp.FinancialStatement.Pdf;

public class TrialBalancePdf(TrialBalancePdfDto model) : IDocument
{
    public TrialBalancePdfDto Model { get; } = model;

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
        //var model = objPdfData[rowcount];
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(4).AlignLeft().Column(column =>
                {
                    column.Item().AlignLeft().Text($"{Model.CompanyInfo.Name}").FontSize(18).Bold();
                    column.Item().AlignLeft().Text($"{Model.CompanyInfo.Address}").FontSize(10);
                    column.Item().AlignLeft().Text($"Contact: {Model.CompanyInfo.CompanyContact}").FontSize(10);
                });
                row.ConstantItem(20);
                row.RelativeItem(4).AlignRight().Column(column =>
                {
                    column.Item().AlignRight().Text("Trial Balance Report").FontSize(18).Bold();
                    column.Item().AlignRight().Text($"Date: {Model.FromMiti}").FontSize(14).Bold();
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
        var headerStyle = TextStyle.Default.Medium().FontSize(8).SemiBold().LineHeight(2.5f);
        var contentStyle = TextStyle.Default.FontSize(7).LineHeight(1.8f);
        var boldContentStyle = TextStyle.Default.FontSize(7).SemiBold().LineHeight(1.8f);

        container.Border(0.5f).Decoration(decoration =>
        {
            // header
            decoration.Before().Background("CCC").Row(row =>
            {
                row.ConstantItem(25).AlignCenter().Text("").Style(headerStyle);
                row.RelativeItem(3).AlignLeft().Text("").Style(headerStyle);
                row.RelativeItem(2).BorderLeft(0.5f).AlignCenter().Text("Opening").Style(headerStyle);
                row.RelativeItem(2).BorderLeft(0.5f).AlignCenter().Text("Transaction").Style(headerStyle);
                row.RelativeItem(2).BorderLeft(0.5f).AlignCenter().Text("Closing").Style(headerStyle);
            });
            decoration.Before().BorderBottom(1).Row(row =>
            {
                row.ConstantItem(25).AlignCenter().Text("S.N.").Style(headerStyle);
                row.RelativeItem(3).BorderLeft(0.5f).AlignCenter().Text("Name").Style(headerStyle);
                row.RelativeItem().BorderLeft(0.5f).AlignCenter().Text("Debit").Style(headerStyle);
                row.RelativeItem().BorderLeft(0.2f).AlignCenter().Text("Credit").Style(headerStyle);
                row.RelativeItem().BorderLeft(0.5f).AlignCenter().Text("Debit").Style(headerStyle);
                row.RelativeItem().BorderLeft(0.2f).AlignCenter().Text("Credit").Style(headerStyle);
                row.RelativeItem().BorderLeft(0.5f).AlignCenter().Text("Debit").Style(headerStyle);
                row.RelativeItem().BorderLeft(0.2f).AlignCenter().Text("Credit").Style(headerStyle);
            });
            // content
            decoration.Content().MinHeight(600).Column(column =>
            {
                var sn = 0;
                foreach (var item in Model.TrialBalanceDetails)
                {
                    if (item.GroupType == TrailBalanceGroupEnum.AccountGroup)
                        sn++;
                    column.Item().BorderBottom(item.GroupType == TrailBalanceGroupEnum.AccountGroup ? 0.5f : 0.2f)
                        .BorderColor("111")
                        .BorderTop(item.GroupType == TrailBalanceGroupEnum.AccountGroup ? 0.5f : 0.2f)
                        .BorderColor("111").Row(row =>
                        {
                            row.ConstantItem(25).AlignCenter()
                                .Text(item.GroupType == TrailBalanceGroupEnum.AccountGroup ? sn + "." : "")
                                .Style(contentStyle);
                            row.RelativeItem(3).BorderLeft(0.5f).AlignLeft().Text($"{item.Name}").Style(
                                item.GroupType == TrailBalanceGroupEnum.AccountGroup ? boldContentStyle : contentStyle);
                            row.RelativeItem().BorderLeft(0.5f).Padding(2).AlignCenter().Text($"{item.OpeningDr}")
                                .Style(contentStyle);
                            row.RelativeItem().BorderLeft(0.2f).Padding(2).AlignCenter().Text($"{item.OpeningCr}")
                                .Style(contentStyle);
                            row.RelativeItem().BorderLeft(0.5f).Padding(2).AlignCenter().Text($"{item.Debit}")
                                .Style(contentStyle);
                            row.RelativeItem().BorderLeft(0.2f).Padding(2).AlignCenter().Text($"{item.Credit}")
                                .Style(contentStyle);
                            row.RelativeItem().BorderLeft(0.5f).Padding(2).AlignCenter().Text($"{item.ClosingDr}")
                                .Style(contentStyle);
                            row.RelativeItem().BorderLeft(0.2f).Padding(2).AlignCenter().Text($"{item.ClosingCr}")
                                .Style(contentStyle);
                        });
                }
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
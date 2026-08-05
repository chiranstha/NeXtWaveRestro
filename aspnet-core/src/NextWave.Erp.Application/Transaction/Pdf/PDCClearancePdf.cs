using NextWave.Erp.Transaction.Dtos;
using NextWave.Erp.Transaction.Enums;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace NextWave.Erp.Transaction.Pdf
{
    public class PDCClearancePdf(GetPdfForPDCClearances model) : IDocument
    {
        public GetPdfForPDCClearances Model { get; } = model;

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
            container.Page(page =>
            {
                page.MarginHorizontal(30);
                page.MarginVertical(50);
                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().AlignLeft().AlignMiddle().Column(column =>
                {
                    if (Model.Logo1 != null) column.Item().AlignLeft().Height(50).Image(Model.Logo1).FitArea();
                });
                if (Model.BranchName != null)
                    if (Model.BranchName.Trim().Length > 0)
                        row.RelativeItem(6).PaddingLeft(10).Column(column =>
                        {
                            column.Item().AlignLeft().Text($"{Model.BranchName}").FontSize(20).Bold();
                            if (Model.BranchAddress != null)
                                column.Item().AlignLeft().Text($"{Model.BranchAddress}").FontSize(11);
                            if (Model.BranchContact != null)
                                column.Item().AlignLeft().Text($"{Model.BranchContact}").FontSize(11);
                        });
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.PaddingVertical(30).Column(column =>
            {
                column.Item().Padding(5).Row(row =>
                {
                    row.ConstantItem(20);
                    row.RelativeItem().Border(1).Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("PDC Clearance").FontSize(18).Bold();
                    });
                    row.ConstantItem(20);
                });
                column.Item().PaddingTop(50).Row(row =>
                {
                    row.RelativeItem(2).AlignRight().Text("Voucher No").FontSize(10);
                    row.RelativeItem(3).Text($": {Model.VoucherNo}").FontSize(11).Bold();

                    row.RelativeItem(2).AlignRight().Text("Party Name").FontSize(10);
                    row.RelativeItem(3).Text($": {Model.LedgerName}").FontSize(11).Bold();

                    row.RelativeItem(2).AlignRight().Text("Cheque Of").FontSize(10);
                    row.RelativeItem(3).Text($": {Model.Bank}").FontSize(11).Bold();
                });
                column.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem(2).AlignRight().Text("Date Miti").FontSize(10);
                    row.RelativeItem(3).Text($": {Model.DateMiti}").FontSize(11).Bold();

                    row.RelativeItem(2).AlignRight().Text("Cheque No").FontSize(10);
                    row.RelativeItem(3).Text($": {Model.ChequeNo}").FontSize(11).Bold();

                    row.RelativeItem(2).AlignRight().Text("Cheque Miti").FontSize(10);
                    row.RelativeItem(3).Text($": {Model.ChequeMiti}").FontSize(11).Bold();
                });
                column.Item().PaddingTop(30).Row(row =>
                {
                    if (Model.AgainstMode == PDCClearanceAgainstMode.PDCPayable)
                        row.RelativeItem(2).AlignRight().Text("Paid From").FontSize(10);
                    else
                        row.RelativeItem(2).AlignRight().Text("Received In").FontSize(10);

                    if (Model.Status == PDCClearanceType.Clearance)
                        row.RelativeItem(3).Text($": {Model.AgainstBank}").FontSize(11).Bold();
                    else
                        row.RelativeItem(3).Text("").FontSize(11).Bold();

                    row.RelativeItem(2).AlignRight().Text("Status").FontSize(10);
                    row.RelativeItem(3).Text($": {Model.Status}").FontSize(11).Bold();

                    row.RelativeItem(2).AlignRight().Text("Amount").FontSize(10);
                    row.RelativeItem(3).Text($": {Model.TotalAmount}").FontSize(11).Bold();
                });
                column.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem(2).AlignRight().Text("Against").FontSize(10);
                    row.RelativeItem(3).Text($": {Model.AgainstMode}").FontSize(11).Bold();
                    row.RelativeItem(2).AlignRight().Text("Narration").FontSize(10);
                    row.RelativeItem(13).Text($": {Model.Narration}").FontSize(11).Bold();
                });
                column.Item().Element(ComposeComments);
            });
        }

        private void ComposeComments(IContainer container)
        {
            container.Extend().AlignBottom().PaddingTop(30).PaddingBottom(20).Row(row =>
            {
                row.RelativeItem(3).BorderTop(1).Column(column =>
                {
                    column.Item().Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("Prepared By").FontSize(12).Medium();
                    });
                });
                row.RelativeItem();
                row.RelativeItem(3).BorderTop(1).Column(column =>
                {
                    column.Item().Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("Verified By").FontSize(12).Medium();
                    });
                });
                row.RelativeItem();
                row.RelativeItem(3).BorderTop(1).Column(column =>
                {
                    column.Item().Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("Approved By").FontSize(12).Medium();
                    });
                });
                row.RelativeItem();
                row.RelativeItem(3).BorderTop(1).Column(column =>
                {
                    column.Item().Text(text =>
                    {
                        text.AlignCenter();
                        text.Span("Reveived By").FontSize(12).Medium();
                    });
                });
            });
        }
    }
}

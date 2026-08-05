using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Storage;

namespace NextWave.Erp.Reporting.AccountingReport.Exporting
{
    public class BookReportExcelExporter(ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IBookReportExcelExporter
    {
        public FileDto ExportToFile(BookReportExcelDto data)
        {
            return CreateExcelPackage(
                "BookReport.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("BookReport");

                    AddMasterHeader(sheet, 1, 1, 0, 12, data.ReportTitle, 14);
                    AddMasterHeader(sheet, 2, 2, 0, 12, "Date : " + data.FromMiti + " To : " + data.ToMiti, 12);
                    AddMasterHeader(sheet, 3, 3, 0, 12, "Opening : " + data.Summary.OpeningBalance + " Closing : " + data.Summary.ClosingBalance, 12);

                    AddHeaderWithMainHeader(
                        sheet,
                        4,
                        "S.N",
                        "Date",
                        "Miti",
                        "Voucher Type",
                        "Voucher No",
                        "Ledger",
                        "Group",
                        "Lines",
                        "Opening",
                        "Debit",
                        "Credit",
                        "In",
                        "Out",
                        "Difference",
                        "Closing",
                        "Age Days",
                        "Current",
                        "1-30",
                        "31-60",
                        "61-90",
                        "90+",
                        "Status",
                        "Remarks"
                    );

                    AddObjectsWithMainHeader(
                        sheet,
                        data.Rows,
                        5,
                        l => l.Sn,
                        l => l.Date.ToString("yyyy-MM-dd"),
                        l => l.DateMiti,
                        l => l.VoucherType,
                        l => l.VoucherNo,
                        l => l.LedgerName,
                        l => l.GroupName,
                        l => l.LineCount,
                        l => l.OpeningBalance,
                        l => l.Debit,
                        l => l.Credit,
                        l => l.InAmount,
                        l => l.OutAmount,
                        l => l.Difference,
                        l => l.ClosingBalance,
                        l => l.AgeDays,
                        l => l.CurrentAmount,
                        l => l.Age1To30,
                        l => l.Age31To60,
                        l => l.Age61To90,
                        l => l.AgeAbove90,
                        l => l.Status,
                        l => l.Remarks
                    );

                    ColumnResize(sheet, 22, 0);
                });
        }
    }
}

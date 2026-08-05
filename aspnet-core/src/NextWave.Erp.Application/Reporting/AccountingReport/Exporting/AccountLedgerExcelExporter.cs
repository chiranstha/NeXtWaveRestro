using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.AccountingReport.Exporting
{
    public class AccountLedgerExcelExporter(ITempFileCacheManager tempFileCacheManager)
    : NpoiExcelExporterBase(tempFileCacheManager), IAccountLedgerExcelExporter
    {
        public FileDto ExportToFile(AccountLedgerReportExcelDto data)
        {
            return CreateExcelPackage(
                "AccountLedgerReport.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("AccountLedgerReport");
                    AddMasterHeader(sheet, 1, 1, 0, 9, "Address : " + data.Address, 14);
                    AddMasterHeader(sheet, 2, 2, 0, 4, "PAN : " + data.Pan, 14);
                    AddMasterHeader(sheet, 2, 2, 5, 9, "Phone no. : " + data.PhoneNo, 14);
                    AddMasterHeader(sheet, 3, 3, 0, 9, "Date : " + data.FromMiti + "  To : " + data.ToMiti, 13);
                    AddMasterHeader(sheet, 4, 4, 0, 9, "Account Ledger Report", 13);

                    AddHeaderWithMainHeader(
                        sheet, 5,
                        "LedgerName",
                        "Address",
                        "PAN",
                        "Phone",
                        "OpeningDr",
                        "OpeningCr",
                        "Debit",
                        "Credit",
                        "BalanceDr",
                        "BalanceCr"
                    );
                    AddObjectsWithMainHeader(
                        sheet, data.Details, 6,
                        l => l.LedgerName,
                        l => l.Address,
                        l => l.Pan,
                        l => l.Phone,
                        l => l.OpeningDr,
                        l => l.OpeningCr,
                        l => l.Debit,
                        l => l.Credit,
                        l => l.BalanceDr,
                        l => l.BalanceCr
                    );
                    ColumnResize(sheet, 8, 0);
                });
        }
    }
}

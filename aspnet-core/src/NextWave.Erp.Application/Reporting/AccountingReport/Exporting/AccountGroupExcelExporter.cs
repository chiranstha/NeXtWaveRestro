using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Storage;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.AccountingReport.Exporting
{
    internal class AccountGroupExcelExporter(ITempFileCacheManager tempFileCacheManager)
    : NpoiExcelExporterBase(tempFileCacheManager), IAccountGroupExcelExporter
    {
        public FileDto ExportToFile(List<AccountGroupReportList> accountGroups)
        {
            return CreateExcelPackage(
                "AccountGroupReport.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("AccountGroupReport");

                    AddHeader(
                        sheet,
                        "S.N.",
                        "GroupName",
                        "Opening",
                        "Debit",
                        "Credit",
                        "Balance"
                    );
                    AddObjects(
                        sheet, accountGroups,
                        //   _ => _.AccountGroupId.ToString(),
                        l => l.Sn,
                        l => l.AccountGroupName,
                        l => l.Opening,
                        l => l.Debit,
                        l => l.Credit,
                        l => l.Balance
                    );
                });
        }
    }
}

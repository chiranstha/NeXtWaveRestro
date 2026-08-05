using System.Collections.Generic;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;

namespace NextWave.Erp.FinancialStatement.Exporting;

public class BalanceSheetExcelExporter(ITempFileCacheManager tempFileCacheManager)
    : NpoiExcelExporterBase(tempFileCacheManager), IBalanceSheetExcelExporter
{
    public FileDto ExportToFile(List<FinancialStatementDetail> balanceSheet)
    {
        return CreateExcelPackage(
            "BalanceSheet.xlsx",
            excelPackage =>
            {
                var sheet = excelPackage.CreateSheet("BalanceSheet");

                AddHeader(
                    sheet,
                    "Name",
                    "OpeningDr",
                    "OpeningCr",
                    "Debit",
                    "Credit",
                    "ClosingDr",
                    "ClosingCr",
                    "GroupType"
                );

                AddObjects(
                    sheet, balanceSheet,
                    d => d.Name,
                    d => d.OpeningDr,
                    d => d.OpeningCr,
                    d => d.Debit,
                    d => d.Credit,
                    d => d.ClosingDr,
                    d => d.ClosingCr,
                    d => d.GroupType
                );
            });
    }
}

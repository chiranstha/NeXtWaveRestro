using System.Collections.Generic;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;

namespace NextWave.Erp.FinancialStatement.Exporting;

public class ProfitLossExcelExporter(ITempFileCacheManager tempFileCacheManager)
    : NpoiExcelExporterBase(tempFileCacheManager), IProfitLossExcelExporter
{
    public FileDto ExportToFile(List<FinancialStatementDetail> profitLoss)
    {
        return CreateExcelPackage(
            "ProfitLoss.xlsx",
            excelPackage =>
            {
                var sheet = excelPackage.CreateSheet("ProfitLoss");

                AddHeader(
                    sheet,
                    "Name",
                    //        "Opening",
                    "Debit",
                    "Credit",
                    //       "Closing",
                    "GroupType"
                );

                AddObjects(
                    sheet, profitLoss,
                    d => d.Name,
                    //    _ => _.Opening,
                    d => d.Debit,
                    d => d.Credit,
                    //    _ => _.Closing,
                    d => d.GroupType
                );
            });
    }
}
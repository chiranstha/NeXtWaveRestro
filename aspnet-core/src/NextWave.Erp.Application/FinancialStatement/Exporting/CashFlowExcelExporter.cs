using System.Collections.Generic;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;

namespace NextWave.Erp.FinancialStatement.Exporting;

public class CashFlowExcelExporter(ITempFileCacheManager tempFileCacheManager)
    : NpoiExcelExporterBase(tempFileCacheManager), ICashFlowExcelExporter
{
    public FileDto ExportToFile(List<CashFlowStatementDetail> cashFlow)
    {
        return CreateExcelPackage(
            "CashFlowReport.xlsx",
            excelPackage =>
            {
                var sheet = excelPackage.CreateSheet("CashFlowReport");

                AddHeader(
                    sheet,
                    "Name",
                    "Current Period",
                    "Previous Period",
                    "GroupType"
                );

                AddObjects(
                    sheet, cashFlow,
                    d => d.Name,
                    d => d.CurrentAmount,
                    d => d.PreviousAmount,
                    d => d.GroupType
                );
            });
    }
}

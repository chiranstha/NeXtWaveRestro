using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Sales.Dtos;
using NextWave.Erp.Storage;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.InventoryReport.Exporting
{

    public class StockReportExcelExporter(ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IStockReportExcelExporter
    {
        public FileDto ExportToFile(List<StockCalculationDto> stockReport)
        {
            return CreateExcelPackage(
                "StockReport.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("StockReport");

                    AddHeader(
                        sheet,
                        "ProductName",
                        "Rate",
                        "OpeningQty",
                        "OpeningValue",
                        "InWardQty",
                        "InWardValue",
                        "OutWardQty",
                        "OutWardValue",
                        "ClosingQty",
                        "ClosingValue"
                    );
                    AddObjects(
                        sheet, stockReport,
                        d => d.ProductName,
                        d => d.Rate,
                        d => d.OpeningStockQtyString,
                        d => d.OpeningStockValue,
                        d => d.InWardQtyString,
                        d => d.InWardValue,
                        d => d.OutWardQtyString,
                        d => d.OutWardValue,
                        d => d.ClosingQtyString,
                        d => d.ClosingValue
                    );
                });
        }
    }
}

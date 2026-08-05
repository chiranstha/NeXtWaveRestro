using NextWave.Erp.Dto;
using NextWave.Erp.Sales.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.InventoryReport.Exporting
{
    public interface IStockReportExcelExporter
    {
        FileDto ExportToFile(List<StockCalculationDto> stockReport);
    }
}

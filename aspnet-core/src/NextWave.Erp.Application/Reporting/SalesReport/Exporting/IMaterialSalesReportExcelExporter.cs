using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.SalesReport.Exporting
{
    public interface IMaterialSalesReportExcelExporter
    {
        FileDto ExportToFile(List<MaterialSalesReportDto> materialSales);
    }
}

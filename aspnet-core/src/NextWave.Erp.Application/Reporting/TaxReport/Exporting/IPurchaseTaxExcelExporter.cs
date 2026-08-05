using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.TaxReport.Exporting
{
    public interface IPurchaseTaxExcelExporter
    {
        FileDto ExportToFile(List<TaxReportDto> salesTax);
    }
}

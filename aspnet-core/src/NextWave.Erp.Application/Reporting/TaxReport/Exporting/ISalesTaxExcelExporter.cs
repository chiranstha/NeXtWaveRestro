using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.TaxReport.Exporting
{
    public interface ISalesTaxExcelExporter
    {
        FileDto ExportToFile(List<TaxReportDto> salesTax);

        FileDto ExportToFileDetail(List<Above1LakhMasterDto> salesTax);
    }
}

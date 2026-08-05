using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.TaxReport.Exporting
{
    public interface IVatSummaryReportExcelExporter
    {
        FileDto ExportToFile(List<FinalVatSummaryReportDto> vatSummary);
    }
}

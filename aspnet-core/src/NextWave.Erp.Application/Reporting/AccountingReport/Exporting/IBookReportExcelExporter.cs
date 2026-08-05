using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;

namespace NextWave.Erp.Reporting.AccountingReport.Exporting
{
    public interface IBookReportExcelExporter
    {
        FileDto ExportToFile(BookReportExcelDto data);
    }
}

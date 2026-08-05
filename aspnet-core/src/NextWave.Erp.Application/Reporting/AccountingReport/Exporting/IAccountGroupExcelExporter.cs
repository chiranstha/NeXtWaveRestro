using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using System.Collections.Generic;


namespace NextWave.Erp.Reporting.AccountingReport.Exporting
{
    public interface IAccountGroupExcelExporter
    {
        FileDto ExportToFile(List<AccountGroupReportList> accountLedger);
    }
}

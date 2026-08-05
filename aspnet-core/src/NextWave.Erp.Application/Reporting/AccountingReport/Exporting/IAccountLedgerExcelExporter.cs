using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.AccountingReport.Exporting
{
    public interface IAccountLedgerExcelExporter
    {
        FileDto ExportToFile(AccountLedgerReportExcelDto data);
    }
}

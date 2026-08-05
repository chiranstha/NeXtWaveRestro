using NextWave.Erp.Accounting.Dtos;
using NextWave.Erp.Dto;
using System.Collections.Generic;

namespace NextWave.Erp.Accounting.Exporting
{
    public interface IAccountLedgersExcelExporter
    {
        FileDto ExportToFile(List<GetAccountLedgerForExportDto> accountLedgers);
    }
}

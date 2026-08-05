using System.Collections.Generic;
using NextWave.Erp.Dto;

namespace NextWave.Erp.FinancialStatement.Exporting;

public interface IBalanceSheetExcelExporter
{
    FileDto ExportToFile(List<FinancialStatementDetail> balanceSheet);
}
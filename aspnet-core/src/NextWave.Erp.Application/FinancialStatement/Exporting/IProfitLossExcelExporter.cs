using System.Collections.Generic;
using NextWave.Erp.Dto;

namespace NextWave.Erp.FinancialStatement.Exporting;

public interface IProfitLossExcelExporter
{
    FileDto ExportToFile(List<FinancialStatementDetail> profitLoss);
}
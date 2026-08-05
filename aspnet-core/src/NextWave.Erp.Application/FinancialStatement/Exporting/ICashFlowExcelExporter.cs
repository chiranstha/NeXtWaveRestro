using System.Collections.Generic;
using NextWave.Erp.Dto;

namespace NextWave.Erp.FinancialStatement.Exporting;

public interface ICashFlowExcelExporter
{
    FileDto ExportToFile(List<CashFlowStatementDetail> cashFlow);
}
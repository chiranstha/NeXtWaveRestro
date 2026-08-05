using System.Collections.Generic;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.ControlPanel.Dtos;
using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;

namespace NextWave.Erp.FinancialStatement.Exporting;

public interface ITrialBalanceExcelExporter
{
    FileDto ExportToFile(List<FinancialStatementDetail> trialBalance, GetBranchForViewDto branchDto);
}

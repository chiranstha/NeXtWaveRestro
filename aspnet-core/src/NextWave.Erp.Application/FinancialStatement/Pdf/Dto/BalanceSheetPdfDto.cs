using System.Collections.Generic;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.ControlPanel.Dtos;
using NextWave.Erp.Reporting.Dto;

namespace NextWave.Erp.FinancialStatement.Pdf.Dto;

public class BalanceSheetPdfDto
{
    public string FromMiti { get; set; }
    public string ToMiti { get; set; }
    public Branch CompanyInfo { get; set; }
    public List<FinancialStatementDetail> BalanceSheetDetails { get; set; }
}

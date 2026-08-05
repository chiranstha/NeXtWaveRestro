using System.Collections.Generic;
using NextWave.Erp.ControlPanel.Dtos;
using NextWave.Erp.Reporting.Dto;

namespace NextWave.Erp.FinancialStatement.Pdf.Dto;

public class TrialBalancePdfDto
{
    public string FromMiti { get; set; }
    public string ToMiti { get; set; }
    public GetBranchForViewDto CompanyInfo { get; set; }
    public List<FinancialStatementDetail> TrialBalanceDetails { get; set; }

}
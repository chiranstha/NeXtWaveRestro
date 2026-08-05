using NextWave.Erp.Sales.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.Dto
{
    public class StockReportPdfDto
    {
        public GetBranchForViewDto CompanyInfo { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public List<StockCalculationDto> StockDetails { get; set; }
    }
}

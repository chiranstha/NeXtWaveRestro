using System;

namespace NextWave.Erp.Reporting.Dto
{
    public class ProductProfitReportDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal InwardRate { get; set; }
        public decimal OutwardRate { get; set; }

        public decimal OpeningStockQty { get; set; }
        public decimal OpeningStockValue { get; set; }
        public decimal InwardQty { get; set; }
        public decimal InwardValue { get; set; }
        public decimal OutwardQty { get; set; }
        public decimal OutwardValue { get; set; }

        public decimal ClosingQty { get; set; }
        public decimal ClosingValue { get; set; }
        public decimal ProfitAmount { get; set; }
        public string UnitName { get; set; }
        public string ProductGroup { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.Dto
{
    public class StockDetailReportDto
    {
        public string ProductName { get; set; }
        public string UnitName { get; set; }
        public decimal OpeningQty { get; set; }
        public decimal OpeningAmount { get; set; }
        public List<StockDetailReportRowDto> Rows { get; set; } = new();
    }

    public class StockDetailReportRowDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string DateMiti { get; set; }
        public string VoucherNo { get; set; }
        public string VoucherType { get; set; }
        public string LedgerName { get; set; }
        public string UnitName { get; set; }
        public decimal InwardQty { get; set; }
        public decimal InwardRate { get; set; }
        public decimal InwardAmount { get; set; }
        public decimal OutwardQty { get; set; }
        public decimal OutwardRate { get; set; }
        public decimal OutwardAmount { get; set; }
        public decimal BalanceQty { get; set; }
        public decimal BalanceAmount { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.Dto
{
    public class PurchaseOrderReportListDto
    {
        public Guid Id { get; set; }
        public int Sn { get; set; }
        public DateTime? Date { get; set; }
        public string DateMiti { get; set; }
        public string VoucherNo { get; set; }
        public decimal TotalAmount { get; set; }
        public string LedgerName { get; set; }
        public bool Status { get; set; }
        public List<PurchaseOrderReportDetailsListDto> Details { get; set; }
    }

    public class PurchaseOrderReportDetailsListDto
    {
        public string ProductName { get; set; }
        public decimal Rate { get; set; }
        public decimal Qty { get; set; }
        public decimal Amount { get; set; }
    }
}

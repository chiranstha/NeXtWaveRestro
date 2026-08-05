using System;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.Dto
{
    public class SalesReportDtoByDate
    {
        public string DateMiti { get; set; }
        public DateTime Date { get; set; }
        public string LedgerName { get; set; }
        public string BillNo { get; set; }
        public string SalesReturnBillNo { get; set; }
        public string PaNumber { get; set; }
        public decimal Total { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public List<SalesReportDtoByDateDetail> Details { get; set; }
    }

    public class SalesReportDtoByDateDetail
    {
        public string Product { get; set; }
        public decimal Qty { get; set; }
        public decimal? Amount { get; set; }
        public decimal TaxAmount { get; set; }
    }
}

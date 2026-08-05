namespace NextWave.Erp.Reporting.Dto
{
    public class MaterialSalesReportDto
    {
        public string FiscalYear { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPan { get; set; }
        public string BillDate { get; set; }
        public string BillNo { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PrintedTime { get; set; }
        public string EnteredBy { get; set; }
        public string PrintedBy { get; set; }
        public decimal VatRefundAmount { get; set; }
        public int TransactionId { get; set; }
        public bool SyncIrd { get; set; }
        public bool BillPrint { get; set; }
        public bool RealTime { get; set; }
        public bool Active { get; set; }
        public string PaymentMethod { get; set; }
    }
}

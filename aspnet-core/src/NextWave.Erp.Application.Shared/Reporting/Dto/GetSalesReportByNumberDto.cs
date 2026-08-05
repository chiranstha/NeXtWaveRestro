namespace NextWave.Erp.Reporting.Dto
{
    public class GetSalesReportByNumberDto
    {
        public int Sn { get; set; }
        public string Name { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string QtyString { get; set; }
        public decimal DiscountAmount { get; set; }
    }
}

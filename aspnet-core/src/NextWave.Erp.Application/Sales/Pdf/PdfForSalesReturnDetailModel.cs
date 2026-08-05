namespace NextWave.Erp.Sales.Pdf
{
    public class PdfForSalesReturnDetailModel
    {
        public int SlNo { get; set; }
        public string ProductName { get; set; }
        public string HsCode { get; set; }
        public decimal Quantity { get; set; }
        public decimal Discount { get; set; }
        public string Unit { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }
}

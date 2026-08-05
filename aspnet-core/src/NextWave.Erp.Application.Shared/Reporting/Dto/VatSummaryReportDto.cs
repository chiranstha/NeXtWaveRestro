namespace NextWave.Erp.Reporting.Dto
{
    public class VatSummaryReportDto
    {
        public MonthListDto TaxableSales { get; set; }
        public MonthListDto Vat { get; set; }
        public MonthListDto ExemptSales { get; set; }
        public MonthListDto ZeroVatSales { get; set; }
        public MonthListDto TotalSales { get; set; }
        public MonthListDto CreditNoteTotal { get; set; }
        public MonthListDto CreditNoteVat { get; set; }
        public MonthListDto NetSales { get; set; }
        public MonthListDto NetVatCollectionOnSales { get; set; }
    }
}

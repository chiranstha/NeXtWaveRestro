namespace NextWave.Erp.Reporting.Dto
{
    public class FinalVatSummaryReportDto
    {
        public MonthListDto TaxableSales { get; set; }
        public MonthListDto SalesVat { get; set; }
        public MonthListDto ExemptSales { get; set; }
        public MonthListDto ZeroVatSales { get; set; }
        public MonthListDto TotalSales { get; set; }
        public MonthListDto CreditNoteTotal { get; set; }
        public MonthListDto CreditNoteVat { get; set; }
        public MonthListDto NetSales { get; set; }
        public MonthListDto NetVatCollectionOnSales { get; set; }
        public MonthListDto TaxablePurchase { get; set; }
        public MonthListDto PurchaseVat { get; set; }
        public MonthListDto CapitalPurchase { get; set; }
        public MonthListDto CapitalPurchaseVat { get; set; }
        public MonthListDto ExemptPurchase { get; set; }
        public MonthListDto TotalPurchase { get; set; }
        public MonthListDto DebitNoteTotal { get; set; }
        public MonthListDto DebitNoteVat { get; set; }
        public MonthListDto NetPurchase { get; set; }
        public MonthListDto NetVatPaidOnPurchase { get; set; }
        public MonthListDto NetOpeningForTheMonth { get; set; }
        public MonthListDto NetVatForTheMonth { get; set; }
        public MonthListDto NetClosingForTheMonth { get; set; }
    }
}

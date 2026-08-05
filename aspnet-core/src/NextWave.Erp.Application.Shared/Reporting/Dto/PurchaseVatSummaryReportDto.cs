namespace NextWave.Erp.Reporting.Dto
{
    public class PurchaseVatSummaryReportDto
    {
        public MonthListDto TaxablePurchase { get; set; }
        public MonthListDto Vat { get; set; }
        public MonthListDto CapitalPurchase { get; set; }
        public MonthListDto CapitalPurchaseVat { get; set; }
        public MonthListDto ExemptPurchase { get; set; }
        public MonthListDto TotalPurchase { get; set; }
        public MonthListDto DebitNoteTotal { get; set; }
        public MonthListDto DebitNoteVat { get; set; }
        public MonthListDto NetPurchase { get; set; }
        public MonthListDto NetVatPaidOnPurchase { get; set; }
        public MonthListDto NetVatForTheMonth { get; set; }
    }
}

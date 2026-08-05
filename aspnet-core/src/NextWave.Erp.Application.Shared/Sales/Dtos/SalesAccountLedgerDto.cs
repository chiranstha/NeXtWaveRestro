namespace NextWave.Erp.Sales.Dtos
{
    public class SalesAccountLedgerDto
    {
        public string Name { get; set; }
        public decimal? CreditLimit { get; set; }
        public int? CreditPeriod { get; set; }
        public string Address { get; set; }
    }
}

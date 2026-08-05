using NextWave.Erp.Enums;

namespace NextWave.Erp.Accounting.Dtos
{
    public class MultipleAccountLedgerCreateList
    {
        public string LedgerName { get; set; }
        public DrOrCr DrOrCr { get; set; }
        public decimal OpeningBalance { get; set; }
        public short? CreditPeriod { get; set; }
        public string PanNumber { get; set; }
        public string MobileNo { get; set; }
        public string BankAcNo { get; set; }

        public decimal CreditLimit { get; set; }
    }
}

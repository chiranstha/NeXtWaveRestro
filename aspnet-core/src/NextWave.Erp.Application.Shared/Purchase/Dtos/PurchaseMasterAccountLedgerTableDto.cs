using System;

namespace NextWave.Erp.Purchase.Dtos
{
    public class PurchaseMasterAccountLedgerTableDto
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }
        public string PanNo { get; set; }
        public string MobileNo { get; set; }
        public string Address { get; set; }
        public int CreditPeriod { get; set; }
        public bool IsCash { get; set; }
    }
}

using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class PaymentVoucherAccountLedgerDropDown
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public bool IsBillByBill { get; set; }
        public string AccountGroup { get; set; }
    }
}

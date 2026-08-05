using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class ReceiptMasterCustomersList
    {
        public Guid LedgerId { get; set; }
        public string DisplayName { get; set; }
        public bool IsBillByBill { get; set; }
    }
}

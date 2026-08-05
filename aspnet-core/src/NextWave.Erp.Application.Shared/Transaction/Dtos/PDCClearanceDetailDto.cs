using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class PDCClearanceDetailDto
    {
        public decimal Amount { get; set; }
        public string ChequeNo { get; set; }
        public string ChequeMiti { get; set; }
        public Guid BankId { get; set; }
        public string VoucherName { get; set; }
    }
}

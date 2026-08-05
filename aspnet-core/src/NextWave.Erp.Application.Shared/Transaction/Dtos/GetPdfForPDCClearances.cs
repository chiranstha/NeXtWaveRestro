using NextWave.Erp.Transaction.Enums;
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetPdfForPDCClearances
    {
        public Guid LedgerId { get; set; }
        public string LedgerName { get; set; }
        public Guid? BranchId { get; set; }
        public string BranchName { get; set; }
        public string BranchAddress { get; set; }
        public string BranchContact { get; set; }
        public byte[] Logo1 { get; set; }
        public string VoucherNo { get; set; }

        public PDCClearanceAgainstMode AgainstMode { get; set; }
        public PDCClearanceType Status { get; set; }
        public DateTime? Date { get; set; }
        public string DateMiti { get; set; }
        public string ChequeMiti { get; set; }
        public string ChequeNo { get; set; }
        public string Bank { get; set; }
        public string AgainstBank { get; set; }
        public string TotalAmountInWord { get; set; }
        public decimal? TotalAmount { get; set; }
        public string Narration { get; set; }
    }
}

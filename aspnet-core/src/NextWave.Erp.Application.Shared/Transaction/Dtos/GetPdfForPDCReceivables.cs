using System;
using System.Collections.Generic;
using System.Text;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetPdfForPDCReceivables
    {
        public Guid LedgerId { get; set; }
        public string LedgerName { get; set; }
        public Guid? BranchId { get; set; }
        public string BranchName { get; set; }
        public string BranchAddress { get; set; }
        public string BranchContact { get; set; }
        public byte[] Logo1 { get; set; }
        public string VoucherNo { get; set; }
        public DateTime? Date { get; set; }
        public string DateMiti { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string ChequeMiti { get; set; }
        public string ChequeNo { get; set; }
        public string Bank { get; set; }
        public string TotalAmountInWord { get; set; }
        public decimal? TotalAmount { get; set; }
        public string Narration { get; set; }
    }
}
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetPdfForContraDetail
    {
        public decimal? Amount { get; set; }
        public string ChequeNo { get; set; }
        public string ChequeDate { get; set; }
        public Guid LedgerId { get; set; }
        public string LedgerName { get; set; }
        public int SlNo { get; set; }
    }
}

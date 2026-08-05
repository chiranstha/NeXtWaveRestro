using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetPdfForReceiptMaster
    {
        public Guid LedgerId { get; set; }
        public string LedgerName { get; set; }
        public string BranchAddress { get; set; }
        public string BranchContact { get; set; }
        public byte[] Logo1 { get; set; }
        public string VoucherNo { get; set; }
        public DateTime? Date { get; set; }
        public string DateMiti { get; set; }
        public string TotalAmountInWord { get; set; }
        public decimal? TotalAmount { get; set; }
        public string Narration { get; set; }

        public List<GetPdfForReceiptDetail> ReceiptDetails { get; set; }
        public string CompanyName { get; set; }
    }
}

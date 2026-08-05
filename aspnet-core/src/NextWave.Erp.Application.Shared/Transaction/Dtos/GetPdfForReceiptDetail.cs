using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetPdfForReceiptDetail
    {
        public int SlNo { get; set; }
        public Guid LedgerId { get; set; }
        public string LedgerName { get; set; }
        public string ChequeNo { get; set; }
        public string ChequeMiti { get; set; }
        public decimal? Amount { get; set; }
    }
}

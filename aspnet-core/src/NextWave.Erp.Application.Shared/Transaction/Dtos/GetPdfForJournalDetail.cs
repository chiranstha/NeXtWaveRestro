using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetPdfForJournalDetail
    {
        public int SlNo { get; set; }
        public Guid LedgerId { get; set; }
        public string LedgerName { get; set; }
        public string ChequeNo { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}

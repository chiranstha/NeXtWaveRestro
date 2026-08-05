using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetContraDetailForEditOutput
    {
        public Guid Id { get; set; }
        public decimal? Amount { get; set; }

        public string ChequeNo { get; set; }

        public string ChequeMiti { get; set; }


        public Guid LedgerId { get; set; }


        public string LedgerName { get; set; }
    }
}

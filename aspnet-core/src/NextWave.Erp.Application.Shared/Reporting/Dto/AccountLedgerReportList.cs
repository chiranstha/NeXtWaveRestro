using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.Dto
{
    public class AccountLedgerReportList
    {
        public Guid AccountLedgerId { get; set; }
        public string LedgerName { get; set; }
        public decimal OpeningDr { get; set; }

        public decimal OpeningCr { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal BalanceDr { get; set; }
        public decimal BalanceCr { get; set; }
        public string Address { get; set; }
        public string Pan { get; set; }
        public string Phone { get; set; }
        public string GroupName { get; set; }
    }
}

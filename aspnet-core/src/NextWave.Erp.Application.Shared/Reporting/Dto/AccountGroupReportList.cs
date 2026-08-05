using System;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.Dto
{
    public class AccountGroupReportList
    {
        public int Sn { get; set; }
        public Guid AccountGroupId { get; set; }
        public string AccountGroupName { get; set; }
        public string Opening { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public string Balance { get; set; }
        public decimal ClosingBlc { get; set; }
        public List<AccountLedgerDetailReportList> Details { get; set; }
    }

    public class AccountLedgerDetailReportList
    {
        public int Sn { get; set; }
        public string LedgerName { get; set; }
        public string Opening { get; set; }
        public decimal OpeningBlc { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public decimal ClosingBlc { get; set; }
        public string Balance { get; set; }
    }
}

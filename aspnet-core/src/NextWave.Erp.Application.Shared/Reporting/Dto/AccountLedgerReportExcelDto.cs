using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.Dto
{
    public class AccountLedgerReportExcelDto
    {
        public string PhoneNo { get; set; }
        public string Address { get; set; }
        public string Pan { get; set; }

        public string FromMiti { get; set; }
        public string ToMiti { get; set; }
        public List<AccountLedgerReportList> Details { get; set; }
    }
}

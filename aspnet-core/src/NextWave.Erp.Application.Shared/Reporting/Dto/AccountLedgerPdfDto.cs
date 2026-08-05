using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.Dto
{
    public class AccountLedgerPdfDto
    {
        public GetBranchForViewDto CompanyInfo { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public byte[] Image1 { get; set; }
        public List<AccountLedgerReportList> LedgerDetails { get; set; }
    }
}

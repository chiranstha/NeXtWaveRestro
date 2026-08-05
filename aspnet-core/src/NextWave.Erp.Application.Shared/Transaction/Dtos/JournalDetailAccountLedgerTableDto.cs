using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction.Dtos
{
    public class JournalDetailAccountLedgerTableDto
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }
        public bool IsBillByBill { get; set; }
    }
}

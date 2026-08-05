using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction.Dtos
{
    public class ReceiptMasterAccountLedgerTableDto
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }
        public string AccountGroup { get; set; }
        public bool IsBillByBill { get; set; }
    }
}

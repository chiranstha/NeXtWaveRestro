using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Accounting.Dtos
{
    public class MultipleAccountLedgerCreateDto
    {
        public Guid AccountGroupId { get; set; }

        public List<MultipleAccountLedgerCreateList> LedgerList { get; set; }
    }
}

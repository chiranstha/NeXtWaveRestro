using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Dtos
{
    public class SalesMasterAccountLedgerTableDto
    {
        public bool IsCash;
        public Guid Id { get; set; }

        public string DisplayName { get; set; }
        public string PanNo { get; set; }
        public string MobileNo { get; set; }
        public int? CreditPeriod { get; set; }
        public string Address { get; set; }
    }
}

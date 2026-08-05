using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class DeleteStockDataDto
    {
        public Guid VoucherTypeId { get; set; }
        public Guid FinancialYearId { get; set; }
        public string VoucherNo { get; set; }
    }
}

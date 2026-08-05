using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.Dto
{
    public class StockReportForProductDto
    {
        public decimal Rate { get; set; }
        public Guid UnitId { get; set; }
        public decimal InWardQty { get; set; }
        public decimal OutWardQty { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal InWardValue { get; set; }
        public decimal OutWardValue { get; set; }
        public string VoucherType { get; set; }
    }
}

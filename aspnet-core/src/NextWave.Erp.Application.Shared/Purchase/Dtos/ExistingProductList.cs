using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class ExistingProductList
    {
        public Guid ProductId { get; set; }
        public decimal Qty { get; set; }
        public decimal Amount { get; set; }
    }
}

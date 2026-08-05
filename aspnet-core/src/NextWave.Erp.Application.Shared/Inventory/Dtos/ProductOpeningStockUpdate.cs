using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Dtos
{
    public class ProductOpeningStockUpdate
    {
        public Guid ProductId { get; set; }
        public Guid UnitId { get; set; }
        public decimal OpeningQty { get; set; }
        public decimal Rate { get; set; }
    }
}

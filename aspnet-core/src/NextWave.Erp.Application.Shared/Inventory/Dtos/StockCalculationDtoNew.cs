using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Dtos
{
    public class StockCalculationDtoNew
    {
        public int SlNo { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string UnitName { get; set; }
        public decimal OpeningStockQty { get; set; }
        public decimal OpeningStockValue { get; set; }
        public decimal InWardQty { get; set; }
        public decimal InWardValue { get; set; }
        public decimal OutWardQty { get; set; }
        public decimal OutWardValue { get; set; }
        public decimal ClosingValue { get; set; }
        public decimal ClosingQty { get; set; }
    }
}

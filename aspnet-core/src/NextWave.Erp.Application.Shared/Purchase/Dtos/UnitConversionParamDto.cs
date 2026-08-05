using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class UnitConversionParamDto
    {
        public Guid ProductId { get; set; }
        public Guid UnitId { get; set; }
        public decimal Rate { get; set; }
        public List<UnitConversionParamDetails> Details { get; set; }
        public decimal Qty { get; set; }
    }

    public class UnitConversionParamDetails
    {
        public Guid UnitId { get; set; }
        public decimal Qty { get; set; }
    }
}

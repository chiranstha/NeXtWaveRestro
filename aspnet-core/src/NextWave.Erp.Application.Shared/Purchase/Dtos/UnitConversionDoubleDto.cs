using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class UnitConversionDoubleDto
    {
        public Guid ProductId { get; set; }
        public Guid UnitId { get; set; }
        public double Rate { get; set; }
        public IEnumerable<UnitConversionParamDetails12> Details { get; set; }
        public double Qty { get; set; }
    }

    public class UnitConversionParamDetails12
    {
        public Guid UnitId { get; set; }
        public double Qty { get; set; }
    }
}

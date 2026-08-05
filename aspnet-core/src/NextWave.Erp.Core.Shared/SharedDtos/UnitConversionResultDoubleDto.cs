using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.SharedDtos
{
    public class UnitConversionResultDoubleDto
    {
        public Guid ProductId { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public double Qty { get; set; }
        public double Rate { get; set; }
    }
}

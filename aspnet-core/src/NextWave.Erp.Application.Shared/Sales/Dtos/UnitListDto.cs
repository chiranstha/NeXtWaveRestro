using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Dtos
{
    public class UnitListDto
    {
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal Rate { get; set; }
        public decimal ConversionRate { get; set; }
    }
}
